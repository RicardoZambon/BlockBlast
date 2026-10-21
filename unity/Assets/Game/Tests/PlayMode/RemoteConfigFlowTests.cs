#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static BlockPuzzle.Game.Tests.TestDrags;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>
    /// Remote Config's app side (master plan 3.1), through App with fakes: fetched after consent; new Classic games, the
    /// ads, the revive and the rating prompt follow it; a game in progress and the Daily don't.
    /// </summary>
    public class RemoteConfigFlowTests
    {
        sealed class FakeRemoteConfig : IRemoteConfig
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
            public int Fetches;

            public void Fetch(Action done)
            {
                Fetches++;
                done();
            }

            public string? Value(string key) => Values.TryGetValue(key, out var value) ? value : null;
        }

        sealed class FakeConsent : IConsentFlow
        {
            Action<ConsentResult>? _finished;
            public bool FinishAtOnce = true;

            public void Request(Action<ConsentResult> finished)
            {
                _finished = finished;
                if (FinishAtOnce) finished(ConsentResult.Unknown);
            }

            public void Finish() => _finished?.Invoke(ConsentResult.Unknown);

            public bool PrivacyOptionsRequired => false;

            public void ShowPrivacyOptions(Action<ConsentResult> closed) => closed(ConsentResult.Unknown);
        }

        sealed class FakeAds : IAdService
        {
            public bool BannerShown;
            public int Interstitials;
            bool _initialized;
            public void Initialize() => _initialized = true;
            public bool IsInterstitialReady => _initialized;
            public bool IsRewardedReady => _initialized;

            public void ShowInterstitial(Action closed)
            {
                Interstitials++;
                closed();
            }

            public void ShowRewarded(Action<bool> finished) => finished(true);
            public void ShowBanner() => BannerShown = true;
            public void HideBanner() => BannerShown = false;
            public event Action<AdRevenue>? RevenuePaid { add { } remove { } }
        }

        sealed class FakeAnalytics : IAnalytics
        {
            public readonly List<(string name, IReadOnlyDictionary<string, object> parameters)> Events =
                new List<(string, IReadOnlyDictionary<string, object>)>();

            public void SetConsent(ConsentResult consent)
            {
            }

            public void Log(string name, IReadOnlyDictionary<string, object>? parameters = null) =>
                Events.Add((name, parameters ?? new Dictionary<string, object>()));

            public IReadOnlyDictionary<string, object> Last(string name) => Events.Last(e => e.name == name).parameters;
        }

        string _folder = "";
        GameObject? _camera;
        App? _app;
        FakeRemoteConfig _remote = new FakeRemoteConfig();
        FakeConsent _consent = new FakeConsent();
        FakeAds _ads = new FakeAds();
        FakeAnalytics _analytics = new FakeAnalytics();

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            _remote = new FakeRemoteConfig();
            _consent = new FakeConsent();
            _ads = new FakeAds();
            _analytics = new FakeAnalytics();
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-remote-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _camera = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = _camera.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            _camera.transform.position = new Vector3(0, 0, -10);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_app != null) Object.Destroy(_app.gameObject);
            if (_camera != null) Object.Destroy(_camera);
            yield return null;
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        App Boot()
        {
            _app = new GameObject("App").AddComponent<App>();
            _app.Boot(_folder, null, _ => { }, Language.English, _consent, _ads, new StandInReviewPrompt(), new StandInReminders(), _analytics,
                remoteConfig: _remote);
            return _app;
        }

        /// <summary>A Classic game one move from its end, after this many finished games.</summary>
        void SaveOneMoveFromGameOver(int gamesFinished)
        {
            var snapshot = TestGames.Position(TestBoards.OneMoveFromGameOver, "i2h", "o3", "o3").ToSnapshot();
            snapshot.GameIndex = gamesFinished;
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = snapshot, NextGameIndex = gamesFinished, TutorialDone = true });
        }

        static IEnumerator EndTheGame(GameController game)
        {
            yield return DragAndRelease(game, 0, PointerFor(game.Layout, PieceCatalog.Standard["i2h"], 0, 1));
            Assert.IsTrue(game.GameOver.IsShown);
        }

        [UnityTest]
        public IEnumerator Nothing_is_fetched_before_consent_finishes()
        {
            _consent.FinishAtOnce = false;
            Boot();
            yield return null;
            Assert.AreEqual(0, _remote.Fetches);

            _consent.Finish();

            Assert.AreEqual(1, _remote.Fetches);
        }

        [UnityTest]
        public IEnumerator A_fetched_profile_tunes_the_next_new_classic_game()
        {
            _remote.Values["gen_profile"] = "hard";
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;

            app.Home.PressPlay();
            yield return null;

            Assert.AreEqual("hard", app.Game!.Game.GeneratorProfile);
            Assert.AreEqual("hard", _analytics.Last("game_start")["gen_profile"]);
        }

        [UnityTest]
        public IEnumerator A_game_in_progress_keeps_the_tuning_it_started_with()
        {
            _remote.Values["gen_profile"] = "hard";
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            Object.Destroy(app.gameObject);   // the app is killed mid-game
            yield return null;

            _remote.Values["gen_profile"] = "gentle";
            var relaunched = Boot();
            yield return null;
            relaunched.Home.PressPlay();   // Continue
            yield return null;

            Assert.AreEqual("hard", relaunched.Game!.Game.GeneratorProfile);
        }

        [UnityTest]
        public IEnumerator Interstitials_can_be_switched_off()
        {
            _remote.Values["ads_inter_enabled"] = "false";
            SaveOneMoveFromGameOver(gamesFinished: 3);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;
            yield return EndTheGame(game);
            game.GameOver.ShowRevive(false);

            game.GameOver.PressPlayAgain();

            Assert.AreEqual(0, _ads.Interstitials);
        }

        [UnityTest]
        public IEnumerator The_banner_can_be_switched_off()
        {
            _remote.Values["ads_banner_enabled"] = "false";
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;

            app.Home.PressPlay();
            yield return null;

            Assert.AreEqual(AppScreen.Game, app.Current);
            Assert.IsFalse(_ads.BannerShown);
        }

        [UnityTest]
        public IEnumerator The_revive_can_be_switched_off()
        {
            _remote.Values["ads_revive_enabled"] = "false";
            SaveOneMoveFromGameOver(gamesFinished: 0);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;

            yield return EndTheGame(app.Game!);

            Assert.IsFalse(app.Game!.GameOver.ReviveOffered);
        }

        [UnityTest]
        public IEnumerator The_Daily_deals_the_same_for_everyone_whatever_Remote_Config_says()
        {
            _remote.Values["gen_profile"] = "hard";
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;

            app.Home.PressDaily();
            yield return null;

            Assert.AreEqual("baseline", app.Daily!.Game.GeneratorProfile);
        }
    }
}
