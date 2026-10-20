#nullable enable
using System.Collections;
using System.IO;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static BlockPuzzle.Game.Tests.TestDrags;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>The app shell through App: Home, Play and Continue, pause, game over, Settings and the back button.</summary>
    public class AppFlowTests
    {
        string _folder = "";
        GameObject? _camera;
        App? _app;

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-apptests-" + System.Guid.NewGuid().ToString("N"));
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
            _app.Boot(_folder);
            return _app;
        }

        [UnityTest]
        public IEnumerator Booting_shows_home_with_play()
        {
            var app = Boot();
            yield return null;

            Assert.AreEqual(AppScreen.Home, app.Current);
            Assert.AreEqual("Play", app.Home.PlayLabel);
            Assert.IsTrue(app.Home.gameObject.activeSelf);
            Assert.IsFalse(app.Settings.gameObject.activeSelf);
            Assert.IsNull(app.Game, "the game screen is built the first time Play is pressed");
        }

        [UnityTest]
        public IEnumerator Play_starts_a_game_and_continue_resumes_it_after_home()
        {
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            Assert.AreEqual(AppScreen.Game, app.Current);
            Assert.IsFalse(app.Home.gameObject.activeSelf);
            var game = app.Game!;
            yield return DragAndRelease(game, 0, PointerFor(game.Layout, game.Game.GetTrayPiece(0)!, 0, 0));
            var board = game.Game.Board;
            int placements = game.Game.Placements;

            game.TogglePause();
            game.Pause.PressHome();
            yield return null;
            Assert.AreEqual(AppScreen.Home, app.Current);
            Assert.IsFalse(game.gameObject.activeSelf);
            Assert.AreEqual("Continue", app.Home.PlayLabel);

            app.Home.PressPlay();
            yield return null;
            Assert.AreSame(game, app.Game);
            Assert.IsTrue(game.gameObject.activeSelf);
            Assert.IsFalse(game.IsPaused);
            Assert.AreEqual(placements, game.Game.Placements);
            Assert.AreEqual(board, game.Game.Board);
        }

        [UnityTest]
        public IEnumerator After_game_over_home_offers_play_not_continue()
        {
            TestGames.Save(SavePath, TestGames.Position(TestBoards.OneMoveFromGameOver, "i2h", "o3", "o3"));
            var app = Boot();
            yield return null;
            Assert.AreEqual("Continue", app.Home.PlayLabel);
            app.Home.PressPlay();
            yield return null;
            var game = app.Game!;

            yield return DragAndRelease(game, 0, PointerFor(game.Layout, PieceCatalog.Standard["i2h"], 0, 1));
            Assert.IsTrue(game.GameOver.IsShown);
            game.GameOver.PressHome();
            yield return null;
            Assert.AreEqual(AppScreen.Home, app.Current);
            Assert.AreEqual("Play", app.Home.PlayLabel);

            app.Home.PressPlay();
            yield return null;
            Assert.IsFalse(game.GameOver.IsShown, "Play starts the next game, never the finished one");
            Assert.IsFalse(game.Game.IsGameOver);
            Assert.AreEqual(0, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator Settings_are_remembered_across_boots()
        {
            var first = Boot();
            yield return null;
            first.Home.PressSettings();
            Assert.AreEqual(AppScreen.Settings, first.Current);
            first.Settings.SetToggle("Music", false);
            first.Settings.SetToggle("Reduced motion", true);
            Object.DestroyImmediate(first.gameObject);   // no quit callbacks: each change was saved when it was made

            var second = Boot();
            yield return null;
            Assert.IsTrue(second.Settings.IsOn("Sound"));
            Assert.IsFalse(second.Settings.IsOn("Music"));
            Assert.IsTrue(second.Settings.IsOn("Haptics"));
            Assert.IsTrue(second.Settings.IsOn("Reduced motion"));
            Assert.AreEqual("Version " + Application.version, second.Settings.VersionText);
        }

        [UnityTest]
        public IEnumerator Back_goes_from_settings_to_home_and_pauses_a_game()
        {
            var app = Boot();
            yield return null;
            app.ShowSettings();
            app.Back();
            Assert.AreEqual(AppScreen.Home, app.Current);
            app.Back();
            Assert.AreEqual(AppScreen.Home, app.Current, "back on Home does nothing");

            app.PlayOrContinue();
            yield return null;
            var game = app.Game!;
            app.Back();
            Assert.IsTrue(game.IsPaused);
            app.Back();
            Assert.IsFalse(game.IsPaused);
            Assert.AreEqual(AppScreen.Game, app.Current);
        }

        [UnityTest]
        public IEnumerator Daily_is_shown_but_disabled()
        {
            var app = Boot();
            yield return null;

            Assert.AreEqual("Daily · soon", app.Home.DailyLabel);
            Assert.IsFalse(app.Home.DailyEnabled);
        }
    }
}
