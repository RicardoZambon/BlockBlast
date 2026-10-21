#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using BlockPuzzle.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockPuzzle.Game
{
    public enum AppScreen
    {
        Home,
        Game,
        Daily,
        Tutorial,
        Settings,
    }

    /// <summary>
    /// The scene's root: owns the save and the settings, and shows one screen at a time. The Classic game screen is
    /// built the first time Play is pressed and keeps its state while hidden. The Daily's screen is built each time
    /// Daily is pressed and destroyed on the way out, so it always deals the day it was opened on; its attempt in
    /// progress is in the save, so it resumes.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        SaveStore? _saveStore;
        SaveData _save = new SaveData();
        SettingsStore? _settingsStore;
        SettingsData _settings = new SettingsData();
        Func<DateTime> _utcNow = () => DateTime.UtcNow;
        Action<string>? _share;
        IConsentFlow _consent = new StandInConsent();
        IAdService _ads = new StandInAds();
        readonly AdPolicySettings _adPolicy = new AdPolicySettings();
        DateTime? _lastFullScreenAd;
        bool _adsStarted;
        ConsentGatedAnalytics _analytics = new ConsentGatedAnalytics(new StandInAnalytics());
        IReviewPrompt _review = new StandInReviewPrompt();
        IReminders _reminders = DefaultReminders();
        int _homeDay;
        bool _booted;

        public HomeScreen Home { get; private set; } = null!;
        public SettingsScreen Settings { get; private set; } = null!;
        public GameController? Game { get; private set; }
        public GameController? Daily { get; private set; }

        /// <summary>The first-time tutorial while it runs; built on a new player's first Play and dropped when it ends.</summary>
        public GameController? Tutorial { get; private set; }

        public AppScreen Current { get; private set; }

        int Today => DailyPuzzle.DayNumber(_utcNow());

        void Start()
        {
            if (!_booted) Boot(Application.persistentDataPath);
        }

        void Update()
        {
            // The Android back button arrives as Escape.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();

            // Home's Daily button changes at midnight UTC, including when the app comes back on a later day.
            if (_booted && Current == AppScreen.Home && Today != _homeDay) ShowHome();
        }

        /// <summary>
        /// Builds the app with its save and settings files in folder. Start calls it unless something called it first.
        /// Tests pass their own clock, share action and language; the app uses the UTC time, DailyShare.Share and the
        /// phone's language.
        /// </summary>
        public void Boot(string folder, Func<DateTime>? utcNow = null, Action<string>? share = null, Language? language = null,
            IConsentFlow? consent = null, IAdService? ads = null, IReviewPrompt? review = null, IReminders? reminders = null,
            IAnalytics? analytics = null)
        {
            if (_booted) return;
            _booted = true;
            Strings.Current = language ?? Strings.FromSystem(Application.systemLanguage);   // before any screen is built
            Application.targetFrameRate = 60;
            if (utcNow != null) _utcNow = utcNow;
            _share = share;
            if (consent != null) _consent = consent;
            if (ads != null) _ads = ads;
            if (review != null) _review = review;
            if (reminders != null) _reminders = reminders;
            if (analytics != null) _analytics = new ConsentGatedAnalytics(analytics);
            _saveStore = new SaveStore(Path.Combine(folder, SaveStore.FileName));
            if (_saveStore.TryRead(out var saved)) _save = saved;
            _settingsStore = new SettingsStore(Path.Combine(folder, SettingsStore.FileName));
            _settings = _settingsStore.Load();

            UiFactory.EnsureEventSystem(transform);
            Home = HomeScreen.Create(transform, PlayOrContinue, PlayDaily, ShowSettings);
            Settings = SettingsScreen.Create(transform, _settings, SaveSettings, ShowHome);
            ShowHome();

            // Consent first: no ad SDK starts, and so none collects anything, before the flow finishes (3.2).
            _consent.Request(StartAds);
        }

        void StartAds()
        {
            if (_adsStarted) return;
            _adsStarted = true;
            _ads.Initialize();
            _analytics.Open();
            _analytics.Log("consent_result", new Dictionary<string, object> { ["gdpr_applies"] = "unknown", ["att_status"] = "unknown" });   // real values come with UMP and ATT (3.2)
            Show(Current);   // the banner, if a game screen is already up
        }

        /// <summary>Whether AdPolicy lets an interstitial show between games now: the grace games, then the cooldown.</summary>
        public bool InterstitialAllowedNow()
        {
            double? since = _lastFullScreenAd == null ? (double?)null : (_utcNow() - _lastFullScreenAd.Value).TotalSeconds;
            return AdPolicy.CanShowInterstitial(_adPolicy, ownsRemoveAds: false, _save.NextGameIndex, since, betweenGames: true);
        }

        /// <summary>Between games: an interstitial when allowed and loaded, then the player's next step, shown or not.</summary>
        void ShowInterstitialThen(Action next)
        {
            if (!_adsStarted || !InterstitialAllowedNow() || !_ads.IsInterstitialReady)
            {
                next();
                return;
            }
            _lastFullScreenAd = _utcNow();
            _ads.ShowInterstitial(next);
        }

        public void ShowHome()
        {
            CloseDaily();
            CloseTutorial();   // left mid-way, it starts over next time
            int today = Today;
            _homeDay = today;
            // Once the Classic screen exists it keeps the shared SaveData current after every move. Before that, only a
            // save that resumes into a game that isn't over is worth a Continue.
            bool classicInProgress = Game != null ? _save.HasGame : ClassicMode.CanContinue(_save);
            Home.Refresh(classicInProgress, DailyMode.StateOn(_save, today), _save.Daily.Record.StreakOn(today));
            Show(AppScreen.Home);
        }

        /// <summary>
        /// Continues the Classic game in progress, or starts the next one, including after a game over. A new player's
        /// first Play runs the tutorial instead, and Classic starts when it ends.
        /// </summary>
        public void PlayOrContinue()
        {
            if (Game == null && TutorialMode.ShouldShow(_save))
            {
                CloseTutorial();
                Tutorial = CreateGame("Tutorial", new TutorialMode(_save));
                _analytics.Log("tutorial_begin");
                Tutorial.Completed += OnTutorialCompleted;
                Show(AppScreen.Tutorial);
                return;
            }
            if (Game == null) Game = CreateGame("Game", new ClassicMode(_save));
            if (Game.GameOver.IsShown) Game.PlayAgain();   // after a game over, or a saved game that was already over
            Show(AppScreen.Game);
        }

        /// <summary>Opens today's Daily, continuing today's attempt. Does nothing once today is played or the clock went back.</summary>
        public void PlayDaily()
        {
            int today = Today;
            if (DailyMode.StateOn(_save, today) == DailyState.Done) return;
            CloseDaily();
            Daily = CreateGame("Daily", new DailyMode(_save, today, Application.productName));
            Show(AppScreen.Daily);
        }

        public void ShowSettings() => Show(AppScreen.Settings);

        /// <summary>The back button: Settings goes back to Home, a game toggles pause, and Home does nothing.</summary>
        public void Back()
        {
            if (Current == AppScreen.Settings) ShowHome();
            else if (Current == AppScreen.Game && Game != null) Game.TogglePause();
            else if (Current == AppScreen.Daily && Daily != null) Daily.TogglePause();
            else if (Current == AppScreen.Tutorial && Tutorial != null) Tutorial.TogglePause();
        }

        void OnTutorialCompleted()
        {
            _analytics.Log("tutorial_complete");
            CloseTutorial();
            PlayOrContinue();   // the save says the tutorial is done now, so this starts Classic
        }

        void CloseTutorial()
        {
            if (Tutorial == null) return;
            Tutorial.gameObject.SetActive(false);
            Destroy(Tutorial.gameObject);
            Tutorial = null;
        }

        GameController CreateGame(string name, GameMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var game = go.AddComponent<GameController>();
            // Wired before Boot, which may already start a game (game_start) or end one (a save that was already over).
            game.HomeRequested += ShowHome;
            game.Ads = _ads;
            game.Analytics = _analytics;
            game.BetweenGames = ShowInterstitialThen;
            game.FullScreenAdShown += () => _lastFullScreenAd = _utcNow();
            if (mode is ClassicMode) game.GameRecorded += () => OnClassicRecorded(game);
            else if (mode is DailyMode) game.GameRecorded += OnDailyRecorded;
            game.Boot(_saveStore!, _save, mode, _share, _settings);
            return game;
        }

        void CloseDaily()
        {
            if (Daily == null) return;
            Daily.gameObject.SetActive(false);   // a drag in progress goes back first
            Destroy(Daily.gameObject);
            Daily = null;
        }

        /// <summary>Shows one screen and hides the others' roots.</summary>
        void Show(AppScreen screen)
        {
            Current = screen;
            if (_adsStarted && (screen == AppScreen.Game || screen == AppScreen.Daily)) _ads.ShowBanner();
            else _ads.HideBanner();
            Home.gameObject.SetActive(screen == AppScreen.Home);
            Settings.gameObject.SetActive(screen == AppScreen.Settings);
            if (Game != null) Game.gameObject.SetActive(screen == AppScreen.Game);
            if (Daily != null) Daily.gameObject.SetActive(screen == AppScreen.Daily);
            if (Tutorial != null) Tutorial.gameObject.SetActive(screen == AppScreen.Tutorial);
        }

        void SaveSettings() => _settingsStore?.Save(_settings);

        static IReminders DefaultReminders()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidReminders();
#else
            return new StandInReminders();
#endif
        }

        /// <summary>A finished Classic game: a rating request after a new best, by RetentionRules (3.7).</summary>
        void OnClassicRecorded(GameController game)
        {
            int today = Today;
            if (!RetentionRules.ShouldAskForRating(game.LastGameSetNewBest, _save.NextGameIndex, _save.RatingAskedDay, today)) return;
            _save.RatingAskedDay = today;   // written with the finished game, right after this
            _review.Request();
        }

        /// <summary>
        /// A finished Daily: after the first one, ask once whether reminders may show. After every one, if they may,
        /// schedule the next reminder, replacing the last.
        /// </summary>
        void OnDailyRecorded()
        {
            var record = _save.Daily.Record;
            _analytics.Log("daily_complete", new Dictionary<string, object> { ["score"] = record.LastScore, ["streak_days"] = record.Streak });
            if (RetentionRules.ShouldAskForReminders(_save.Daily.Record, _save.RemindersAsked))
            {
                _save.RemindersAsked = true;   // written with the finished Daily
                StartCoroutine(_reminders.RequestPermission(allowed =>
                {
                    _save.RemindersOn = allowed;
                    if (allowed) ScheduleReminder();
                    Persist();   // the answer can come after the Daily was saved
                }));
            }
            else if (_save.RemindersOn)
            {
                ScheduleReminder();
            }
        }

        void ScheduleReminder()
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(_utcNow());
            _reminders.Schedule(RetentionRules.NextReminderUtc(_save.Daily.Record.LastDay, offset), Application.productName,
                Strings.Get("notify.daily_ready"));
        }

        /// <summary>Writes the save outside a game screen; a failure is logged, like the game's own saves.</summary>
        void Persist()
        {
            try
            {
                _saveStore?.Write(_save);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Couldn't save: " + e.Message);
            }
        }
    }
}
