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
    /// <summary>The master plan's analytics events (3.1), logged through App with fakes, and nothing before consent.</summary>
    public class AnalyticsFlowTests
    {
        sealed class FakeAnalytics : IAnalytics
        {
            public readonly List<(string name, IReadOnlyDictionary<string, object> parameters)> Events =
                new List<(string, IReadOnlyDictionary<string, object>)>();

            public void Log(string name, IReadOnlyDictionary<string, object>? parameters = null) =>
                Events.Add((name, parameters ?? new Dictionary<string, object>()));

            public IReadOnlyDictionary<string, object> Last(string name) => Events.Last(e => e.name == name).parameters;

            public string[] Names => Events.Select(e => e.name).ToArray();
        }

        sealed class FakeConsent : IConsentFlow
        {
            Action? _finished;
            public bool FinishAtOnce = true;

            public void Request(Action finished)
            {
                _finished = finished;
                if (FinishAtOnce) finished();
            }

            public void Finish() => _finished?.Invoke();
        }

        sealed class FakeAds : IAdService
        {
            public bool RewardedLoaded;
            bool _initialized;
            public void Initialize() => _initialized = true;
            public bool IsInterstitialReady => false;
            public bool IsRewardedReady => _initialized && RewardedLoaded;
            public void ShowInterstitial(Action closed) => closed();
            public void ShowRewarded(Action<bool> finished) => finished(true);
            public void ShowBanner() { }
            public void HideBanner() { }
        }

        const int Day = 20747;   // 2026-10-21
        static readonly DateTime Now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        string _folder = "";
        GameObject? _camera;
        App? _app;
        FakeAnalytics _analytics = new FakeAnalytics();
        FakeConsent _consent = new FakeConsent();
        FakeAds _ads = new FakeAds();

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            _analytics = new FakeAnalytics();
            _consent = new FakeConsent();
            _ads = new FakeAds();
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-analytics-" + Guid.NewGuid().ToString("N"));
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
            _app.Boot(_folder, () => Now, _ => { }, Language.English, _consent, _ads, new StandInReviewPrompt(), new StandInReminders(), _analytics);
            return _app;
        }

        static GameSnapshot OneMoveFromGameOver(int gameIndex)
        {
            var snapshot = TestGames.Position(TestBoards.OneMoveFromGameOver, "i2h", "o3", "o3").ToSnapshot();
            snapshot.GameIndex = gameIndex;
            return snapshot;
        }

        static IEnumerator EndTheGame(GameController game)
        {
            yield return DragAndRelease(game, 0, PointerFor(game.Layout, PieceCatalog.Standard["i2h"], 0, 1));
            Assert.IsTrue(game.GameOver.IsShown);
        }

        [UnityTest]
        public IEnumerator Nothing_is_logged_before_consent_finishes()
        {
            _consent.FinishAtOnce = false;
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();   // a new Classic game: game_start, while consent is still open
            yield return null;
            Assert.IsEmpty(_analytics.Events);

            _consent.Finish();

            CollectionAssert.AreEqual(new[] { "consent_result" }, _analytics.Names);
        }

        [UnityTest]
        public IEnumerator A_classic_game_logs_its_end_and_the_next_one_its_start()
        {
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = OneMoveFromGameOver(gameIndex: 2), NextGameIndex = 2, TutorialDone = true });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;

            yield return EndTheGame(game);
            var end = _analytics.Last("game_end");
            Assert.AreEqual("classic", end["mode"]);
            Assert.AreEqual(game.Game.Score, end["score"]);
            Assert.AreEqual(1, end["placements"]);
            Assert.AreEqual(0, end["lines"]);
            Assert.AreEqual(0, end["max_streak"]);
            Assert.AreEqual(false, end["revived"]);
            Assert.GreaterOrEqual((int)end["duration_s"], 0);

            game.GameOver.PressPlayAgain();
            var start = _analytics.Last("game_start");
            Assert.AreEqual("classic", start["mode"]);
            Assert.AreEqual(3, start["game_index"]);
            Assert.AreEqual("baseline", start["gen_profile"]);
        }

        [UnityTest]
        public IEnumerator The_tutorial_logs_its_begin_and_complete()
        {
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            Assert.Contains("tutorial_begin", _analytics.Names);

            app.Tutorial!.Hint!.PressSkip();
            yield return null;

            CollectionAssert.IsSubsetOf(new[] { "tutorial_begin", "tutorial_complete", "game_start" }, _analytics.Names);
            Assert.Less(Array.IndexOf(_analytics.Names, "tutorial_complete"), Array.LastIndexOf(_analytics.Names, "game_start"));
        }

        [UnityTest]
        public IEnumerator The_daily_logs_its_end_completion_and_share()
        {
            new SaveStore(SavePath).Write(new SaveData
            {
                TutorialDone = true,
                Daily = new DailySave { HasGame = true, GameDay = Day, Game = OneMoveFromGameOver(gameIndex: 0) },
            });
            var app = Boot();
            yield return null;
            app.Home.PressDaily();
            yield return null;
            var daily = app.Daily!;

            yield return EndTheGame(daily);
            daily.GameOver.PressShare();

            Assert.AreEqual("daily", _analytics.Last("game_end")["mode"]);
            var complete = _analytics.Last("daily_complete");
            Assert.AreEqual(daily.Game.Score, complete["score"]);
            Assert.AreEqual(1, complete["streak_days"]);
            Assert.AreEqual("daily_result", _analytics.Last("share")["content_type"]);
        }

        [UnityTest]
        public IEnumerator A_revive_logs_its_offer_and_acceptance()
        {
            _ads.RewardedLoaded = true;
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = OneMoveFromGameOver(gameIndex: 4), NextGameIndex = 4, TutorialDone = true });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;

            yield return EndTheGame(game);
            Assert.AreEqual(4, _analytics.Last("revive_offer")["game_index"]);
            Assert.IsFalse(_analytics.Names.Contains("game_end"), "the game isn't over yet while a revive is offered");

            game.GameOver.PressRevive();

            Assert.AreEqual(4, _analytics.Last("revive_accept")["game_index"]);
        }
    }
}
