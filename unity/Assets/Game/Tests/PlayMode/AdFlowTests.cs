#nullable enable
using System;
using System.Collections;
using System.IO;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static BlockPuzzle.Game.Tests.TestDrags;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>Ads through App, with fakes: consent first, interstitials between games, the rewarded revive and the banner.</summary>
    public class AdFlowTests
    {
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
            public bool Initialized;
            public bool InterstitialLoaded = true;
            public bool RewardedLoaded = true;
            public bool Rewards = true;
            public int Interstitials;
            public int Rewarded;
            public bool BannerShown;

            public void Initialize() => Initialized = true;
            public bool IsInterstitialReady => Initialized && InterstitialLoaded;
            public bool IsRewardedReady => Initialized && RewardedLoaded;

            public void ShowInterstitial(Action closed)
            {
                Interstitials++;
                closed();
            }

            public void ShowRewarded(Action<bool> finished)
            {
                Rewarded++;
                finished(Rewards);
            }

            public void ShowBanner() => BannerShown = true;
            public void HideBanner() => BannerShown = false;
            public event Action<AdRevenue>? RevenuePaid { add { } remove { } }
        }

        string _folder = "";
        GameObject? _camera;
        App? _app;
        DateTime _now;
        FakeConsent _consent = new FakeConsent();
        FakeAds _ads = new FakeAds();

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            // NUnit keeps one instance of this class for all its tests, so each test gets fresh fakes and a fresh clock.
            _now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
            _consent = new FakeConsent();
            _ads = new FakeAds();
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-adtests-" + Guid.NewGuid().ToString("N"));
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
            _app.Boot(_folder, () => _now, _ => { }, Language.English, _consent, _ads);
            return _app;
        }

        /// <summary>A Classic game one move from its end, after this many finished games.</summary>
        void SaveOneMoveFromGameOver(int gamesFinished, bool revived = false)
        {
            var snapshot = TestGames.Position(TestBoards.OneMoveFromGameOver, "i2h", "o3", "o3").ToSnapshot();
            snapshot.GameIndex = gamesFinished;
            snapshot.Revived = revived;
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = snapshot, NextGameIndex = gamesFinished, TutorialDone = true });
        }

        static IEnumerator EndTheGame(GameController game)
        {
            yield return DragAndRelease(game, 0, PointerFor(game.Layout, PieceCatalog.Standard["i2h"], 0, 1));
            Assert.IsTrue(game.GameOver.IsShown);
        }

        [UnityTest]
        public IEnumerator Ads_start_only_after_consent_finishes()
        {
            _consent.FinishAtOnce = false;
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;

            Assert.IsFalse(_ads.Initialized, "nothing starts before consent");
            Assert.IsFalse(_ads.BannerShown);
            Assert.AreEqual(AppScreen.Game, app.Current, "the game doesn't wait for consent");

            _consent.Finish();

            Assert.IsTrue(_ads.Initialized);
            Assert.IsTrue(_ads.BannerShown, "the banner comes up on the game screen already showing");
        }

        [UnityTest]
        public IEnumerator An_interstitial_shows_between_games_after_the_grace_games_then_waits_out_the_cooldown()
        {
            SaveOneMoveFromGameOver(gamesFinished: 3);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;
            yield return EndTheGame(game);
            game.GameOver.ShowRevive(false);

            game.GameOver.PressPlayAgain();

            Assert.AreEqual(1, _ads.Interstitials);
            Assert.IsFalse(game.GameOver.IsShown, "the next game starts once the ad closes");
            Assert.IsFalse(app.InterstitialAllowedNow(), "the cooldown holds the next one back");
            _now = _now.AddSeconds(91);
            Assert.IsTrue(app.InterstitialAllowedNow());
        }

        [UnityTest]
        public IEnumerator No_interstitial_in_the_grace_games()
        {
            SaveOneMoveFromGameOver(gamesFinished: 0);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;
            yield return EndTheGame(game);

            game.GameOver.PressHome();

            Assert.AreEqual(0, _ads.Interstitials);
            Assert.AreEqual(AppScreen.Home, app.Current);
        }

        [UnityTest]
        public IEnumerator The_revive_continues_the_game_once()
        {
            SaveOneMoveFromGameOver(gamesFinished: 0);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;
            yield return EndTheGame(game);
            Assert.IsTrue(game.GameOver.ReviveOffered);

            game.GameOver.PressRevive();

            Assert.AreEqual(1, _ads.Rewarded);
            Assert.IsFalse(game.GameOver.IsShown);
            Assert.IsFalse(game.Game.IsGameOver, "a fresh tray that fits");
            Assert.IsTrue(game.Game.Revived);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsTrue(saved.HasGame, "the revived game goes on, so it isn't recorded as finished");
            Assert.AreEqual(0, saved.NextGameIndex);
        }

        [UnityTest]
        public IEnumerator A_revived_game_that_ends_again_offers_no_second_revive()
        {
            SaveOneMoveFromGameOver(gamesFinished: 0, revived: true);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;

            yield return EndTheGame(game);

            Assert.IsFalse(game.GameOver.ReviveOffered);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsFalse(saved.HasGame, "with no revive to offer, the game is recorded at once");
            Assert.AreEqual(1, saved.NextGameIndex);
        }

        [UnityTest]
        public IEnumerator No_revive_without_a_loaded_ad_and_a_skipped_ad_gives_nothing()
        {
            _ads.RewardedLoaded = false;
            SaveOneMoveFromGameOver(gamesFinished: 0);
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;
            yield return EndTheGame(game);
            Assert.IsFalse(game.GameOver.ReviveOffered);

            game.GameOver.PressPlayAgain();
            yield return null;
            _ads.RewardedLoaded = true;
            _ads.Rewards = false;
            SaveOneMoveFromGameOver(gamesFinished: 1);
            Object.DestroyImmediate(app.gameObject);
            var second = Boot();
            yield return null;
            second.Home.PressPlay();
            yield return null;
            var again = second.Game!;
            yield return EndTheGame(again);

            again.GameOver.PressRevive();

            Assert.IsTrue(again.GameOver.IsShown, "a skipped ad leaves game over");
            Assert.IsFalse(again.GameOver.ReviveOffered, "and the offer is used up");
            Assert.IsTrue(again.Game.IsGameOver);
        }

        [UnityTest]
        public IEnumerator The_banner_shows_on_game_screens_only()
        {
            new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });
            var app = Boot();
            yield return null;
            Assert.IsFalse(_ads.BannerShown, "not on Home");

            app.Home.PressPlay();
            yield return null;
            Assert.IsTrue(_ads.BannerShown);
            app.Game!.TogglePause();
            app.Game.Pause.PressHome();
            yield return null;
            Assert.IsFalse(_ads.BannerShown);

            app.Home.PressDaily();
            yield return null;
            Assert.IsTrue(_ads.BannerShown, "the Daily is a game screen too");
            app.ShowHome();
            app.ShowSettings();
            Assert.IsFalse(_ads.BannerShown, "not in Settings");
        }
    }
}
