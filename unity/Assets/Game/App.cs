#nullable enable
using System;
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
        int _homeDay;
        bool _booted;

        public HomeScreen Home { get; private set; } = null!;
        public SettingsScreen Settings { get; private set; } = null!;
        public GameController? Game { get; private set; }
        public GameController? Daily { get; private set; }
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
        /// Tests pass their own clock and share action; the app uses the UTC time and DailyShare.Share.
        /// </summary>
        public void Boot(string folder, Func<DateTime>? utcNow = null, Action<string>? share = null)
        {
            if (_booted) return;
            _booted = true;
            Application.targetFrameRate = 60;
            if (utcNow != null) _utcNow = utcNow;
            _share = share;
            _saveStore = new SaveStore(Path.Combine(folder, SaveStore.FileName));
            if (_saveStore.TryRead(out var saved)) _save = saved;
            _settingsStore = new SettingsStore(Path.Combine(folder, SettingsStore.FileName));
            _settings = _settingsStore.Load();

            UiFactory.EnsureEventSystem(transform);
            Home = HomeScreen.Create(transform, PlayOrContinue, PlayDaily, ShowSettings);
            Settings = SettingsScreen.Create(transform, _settings, SaveSettings, ShowHome);
            ShowHome();
        }

        public void ShowHome()
        {
            CloseDaily();
            int today = Today;
            _homeDay = today;
            // Once the Classic screen exists it keeps the shared SaveData current after every move. Before that, only a
            // save that resumes into a game that isn't over is worth a Continue.
            bool classicInProgress = Game != null ? _save.HasGame : ClassicMode.CanContinue(_save);
            Home.Refresh(classicInProgress, DailyMode.StateOn(_save, today), _save.Daily.Record.StreakOn(today));
            Show(AppScreen.Home);
        }

        /// <summary>Continues the Classic game in progress, or starts the next one, including after a game over.</summary>
        public void PlayOrContinue()
        {
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
        }

        GameController CreateGame(string name, GameMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var game = go.AddComponent<GameController>();
            game.Boot(_saveStore!, _save, mode, _share);
            game.HomeRequested += ShowHome;
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
            Home.gameObject.SetActive(screen == AppScreen.Home);
            Settings.gameObject.SetActive(screen == AppScreen.Settings);
            if (Game != null) Game.gameObject.SetActive(screen == AppScreen.Game);
            if (Daily != null) Daily.gameObject.SetActive(screen == AppScreen.Daily);
        }

        void SaveSettings() => _settingsStore?.Save(_settings);
    }
}
