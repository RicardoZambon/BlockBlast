#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static BlockPuzzle.Game.Tests.TestDrags;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>The rating prompt and the Daily reminder through App, with fakes (master plan 3.7).</summary>
    public class RetentionFlowTests
    {
        sealed class FakeReview : IReviewPrompt
        {
            public int Requests;
            public void Request() => Requests++;
        }

        sealed class FakeReminders : IReminders
        {
            public bool Allow = true;
            public int Asked;
            public readonly List<DateTime> Scheduled = new List<DateTime>();

            public IEnumerator RequestPermission(Action<bool> done)
            {
                Asked++;
                done(Allow);
                yield break;
            }

            public void Schedule(DateTime fireUtc, string title, string body) => Scheduled.Add(fireUtc);
        }

        /// <summary>No ads, so a game over is recorded at once instead of waiting on a revive offer.</summary>
        sealed class NoAds : IAdService
        {
            public void Initialize() { }
            public bool IsInterstitialReady => false;
            public bool IsRewardedReady => false;
            public void ShowInterstitial(Action closed) => closed();
            public void ShowRewarded(Action<bool> finished) => finished(false);
            public void ShowBanner() { }
            public void HideBanner() { }
            public event Action<AdRevenue>? RevenuePaid { add { } remove { } }
        }

        const int Day = 20747;   // 2026-10-21
        static readonly DateTime Now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        string _folder = "";
        GameObject? _camera;
        App? _app;
        FakeReview _review = new FakeReview();
        FakeReminders _reminders = new FakeReminders();

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            _review = new FakeReview();
            _reminders = new FakeReminders();
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-retention-" + Guid.NewGuid().ToString("N"));
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
            _app.Boot(_folder, () => Now, _ => { }, Language.English, new StandInConsent(), new NoAds(), _review, _reminders);
            return _app;
        }

        static GameSnapshot OneMoveFromGameOver(int gameIndex = 0)
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
        public IEnumerator A_new_best_after_five_games_asks_for_a_rating()
        {
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = OneMoveFromGameOver(gameIndex: 4), NextGameIndex = 4, TutorialDone = true });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;

            yield return EndTheGame(app.Game!);

            Assert.AreEqual(1, _review.Requests);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.AreEqual(Day, saved.RatingAskedDay);
        }

        [UnityTest]
        public IEnumerator No_rating_without_a_new_best()
        {
            new SaveStore(SavePath).Write(new SaveData
            {
                HasGame = true, Game = OneMoveFromGameOver(gameIndex: 9), NextGameIndex = 9, BestScore = 5000, TutorialDone = true,
            });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;

            yield return EndTheGame(app.Game!);

            Assert.AreEqual(0, _review.Requests);
        }

        static SaveData DailyInProgress(DailyRecord record) => new SaveData
        {
            TutorialDone = true,
            Daily = new DailySave { HasGame = true, GameDay = Day, Game = OneMoveFromGameOver(), Record = record },
        };

        [UnityTest]
        public IEnumerator The_first_daily_asks_for_reminders_and_schedules_one()
        {
            new SaveStore(SavePath).Write(DailyInProgress(new DailyRecord()));
            var app = Boot();
            yield return null;
            app.Home.PressDaily();
            yield return null;

            yield return EndTheGame(app.Daily!);

            Assert.AreEqual(1, _reminders.Asked);
            var offset = TimeZoneInfo.Local.GetUtcOffset(Now);
            CollectionAssert.AreEqual(new[] { RetentionRules.NextReminderUtc(Day, offset) }, _reminders.Scheduled);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsTrue(saved.RemindersAsked);
            Assert.IsTrue(saved.RemindersOn);
        }

        [UnityTest]
        public IEnumerator A_later_daily_reschedules_without_asking_again()
        {
            var save = DailyInProgress(new DailyRecord { LastDay = Day - 1, Streak = 1, BestStreak = 1 });
            save.RemindersAsked = true;
            save.RemindersOn = true;
            new SaveStore(SavePath).Write(save);
            var app = Boot();
            yield return null;
            app.Home.PressDaily();
            yield return null;

            yield return EndTheGame(app.Daily!);

            Assert.AreEqual(0, _reminders.Asked);
            Assert.AreEqual(1, _reminders.Scheduled.Count);
        }

        [UnityTest]
        public IEnumerator A_refused_permission_is_never_asked_again_and_schedules_nothing()
        {
            var save = DailyInProgress(new DailyRecord { LastDay = Day - 1, Streak = 1, BestStreak = 1 });
            save.RemindersAsked = true;
            save.RemindersOn = false;
            new SaveStore(SavePath).Write(save);
            var app = Boot();
            yield return null;
            app.Home.PressDaily();
            yield return null;

            yield return EndTheGame(app.Daily!);

            Assert.AreEqual(0, _reminders.Asked);
            Assert.AreEqual(0, _reminders.Scheduled.Count);
        }
    }
}
