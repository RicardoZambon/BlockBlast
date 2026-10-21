#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using static BlockPuzzle.Game.Tests.TestDrags;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>The app shell through App: Home, Play and Continue, the Daily, pause, game over, Settings and back.</summary>
    public class AppFlowTests
    {
        static readonly DateTime October20 = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        const int Day = 20746;   // October20's day number

        string _folder = "";
        GameObject? _camera;
        App? _app;
        DateTime _now;
        readonly List<string> _shared = new List<string>();

        string SavePath => Path.Combine(_folder, SaveStore.FileName);

        [SetUp]
        public void SetUp()
        {
            _now = October20;
            _shared.Clear();
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

        /// <summary>An App in this test's folder, on this test's clock, sharing into this test's list.</summary>
        App Boot()
        {
            _app = new GameObject("App").AddComponent<App>();
            _app.Boot(_folder, () => _now, _shared.Add);
            return _app;
        }

        /// <summary>A new player who already did the tutorial, so Play goes straight to Classic.</summary>
        void MarkTutorialDone() => new SaveStore(SavePath).Write(new SaveData { TutorialDone = true });

        static IEnumerator PlayGuidedMove(GameController tutorial)
        {
            var guide = tutorial.CurrentGuide!.Value;
            var shape = tutorial.Game.GetTrayPiece(guide.Slot)!;
            yield return DragAndRelease(tutorial, guide.Slot, PointerFor(tutorial.Layout, shape, guide.Row, guide.Col));
        }

        void SaveDaily(DailyRecord record, GameSession? attempt = null, int attemptDay = Day)
        {
            var save = new SaveData();
            save.Daily.Record = record;
            if (attempt != null)
            {
                save.Daily.HasGame = true;
                save.Daily.GameDay = attemptDay;
                save.Daily.Game = attempt.ToSnapshot();
            }
            new SaveStore(SavePath).Write(save);
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
            MarkTutorialDone();
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
            Assert.AreEqual(1, placements, "a new game would have none, so Continue must keep this one");

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
            app.Back();
            Assert.IsFalse(game.IsPaused, "back on game over does nothing");
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
        public IEnumerator A_saved_game_that_is_already_over_offers_play_and_starts_a_new_game()
        {
            TestGames.Save(SavePath, TestGames.Position(TestBoards.Checkerboard, "i2h", "o3", "o3"), gameIndex: 2);
            var app = Boot();
            yield return null;
            Assert.AreEqual("Play", app.Home.PlayLabel);

            app.Home.PressPlay();
            yield return null;

            var game = app.Game!;
            Assert.IsFalse(game.GameOver.IsShown);
            Assert.IsFalse(game.Game.IsGameOver);
            Assert.AreEqual(3, game.Game.GameIndex, "the finished game still counts");
        }

        [UnityTest]
        public IEnumerator A_saved_game_that_cannot_resume_offers_play()
        {
            var snapshot = TestGames.Position(Board.Empty, "dot", "o2", "i3h").ToSnapshot();
            snapshot.Tray = new[] { "no-such-piece", "o2", "i3h" };   // as if a later version dropped a piece
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = snapshot });

            var app = Boot();
            yield return null;

            Assert.AreEqual("Play", app.Home.PlayLabel);
        }

        [UnityTest]
        public IEnumerator A_damaged_settings_file_still_boots_with_the_game_intact()
        {
            TestGames.Save(SavePath, TestGames.Position(Board.Empty, "dot", "o2", "i3h"));
            File.WriteAllText(Path.Combine(_folder, SettingsStore.FileName), "{ not json");

            var app = Boot();
            yield return null;
            Assert.IsTrue(app.Settings.IsOn("sound"));
            Assert.IsFalse(app.Settings.IsOn("reduced_motion"));
            Assert.AreEqual("Continue", app.Home.PlayLabel);

            app.Home.PressPlay();
            yield return null;
            Assert.AreEqual("dot", app.Game!.Game.GetTrayPiece(0)!.Id);
        }

        [UnityTest]
        public IEnumerator The_escape_key_goes_back()
        {
            // Batch mode has no focused Game view, so for this test all input goes to the game, as it does on a phone.
            // The project has no Input System settings asset, so these are the defaults, changed in memory and put back.
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>();   // the Android back button arrives as Escape
            keyboard.MakeCurrent();
            try
            {
                var app = Boot();
                yield return null;
                app.ShowSettings();

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;

                Assert.AreEqual(AppScreen.Home, app.Current);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = background;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = editorBehavior;
#endif
            }
        }

        [UnityTest]
        public IEnumerator Settings_are_remembered_across_boots()
        {
            var first = Boot();
            yield return null;
            first.Home.PressSettings();
            Assert.AreEqual(AppScreen.Settings, first.Current);
            first.Settings.SetToggle("music", false);
            first.Settings.SetToggle("reduced_motion", true);
            Object.DestroyImmediate(first.gameObject);   // no quit callbacks: each change was saved when it was made

            var second = Boot();
            yield return null;
            Assert.IsTrue(second.Settings.IsOn("sound"));
            Assert.IsFalse(second.Settings.IsOn("music"));
            Assert.IsTrue(second.Settings.IsOn("haptics"));
            Assert.IsTrue(second.Settings.IsOn("reduced_motion"));
            Assert.AreEqual("Version " + Application.version, second.Settings.VersionText);
        }

        [UnityTest]
        public IEnumerator Back_goes_from_settings_to_home_and_pauses_a_game()
        {
            MarkTutorialDone();
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
        public IEnumerator The_first_play_runs_the_tutorial_then_classic()
        {
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            Assert.AreEqual(AppScreen.Tutorial, app.Current);
            var tutorial = app.Tutorial!;
            Assert.AreEqual("Drag the piece onto the board", tutorial.Hint!.Text);
            Assert.IsTrue(tutorial.Hand!.IsShown);

            yield return PlayGuidedMove(tutorial);
            Assert.AreEqual("Fill a row to clear it", tutorial.Hint.Text);
            yield return PlayGuidedMove(tutorial);
            Assert.AreEqual("Columns clear too", tutorial.Hint.Text);
            yield return PlayGuidedMove(tutorial);
            yield return null;

            Assert.AreEqual(AppScreen.Game, app.Current, "Classic starts as soon as the tutorial ends");
            Assert.IsNull(app.Tutorial);
            Assert.AreEqual(0, app.Game!.Game.Placements);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsTrue(saved.TutorialDone);
        }

        [UnityTest]
        public IEnumerator A_drop_off_the_guide_is_refused()
        {
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var tutorial = app.Tutorial!;
            var shape = tutorial.Game.GetTrayPiece(1)!;
            var board = tutorial.Game.Board;

            yield return DragAndRelease(tutorial, 1, PointerFor(tutorial.Layout, shape, 5, 0));   // free, but not the guided spot
            yield return new WaitForSeconds(DragController.ReturnSeconds + 0.05f);

            // An accepted drop would move on to step 2: a new game on a new board, with a bar in the tray.
            Assert.AreEqual(board, tutorial.Game.Board);
            Assert.AreEqual("o2", tutorial.Game.GetTrayPiece(1)!.Id);
            Assert.AreEqual("Drag the piece onto the board", tutorial.Hint!.Text);
        }

        [UnityTest]
        public IEnumerator Skip_goes_straight_to_classic()
        {
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;

            app.Tutorial!.Hint!.PressSkip();
            yield return null;

            Assert.AreEqual(AppScreen.Game, app.Current);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsTrue(saved.TutorialDone);
        }

        [UnityTest]
        public IEnumerator A_returning_player_skips_the_tutorial()
        {
            new SaveStore(SavePath).Write(new SaveData { BestScore = 120, NextGameIndex = 4 });   // played before the tutorial existed
            var app = Boot();
            yield return null;

            app.Home.PressPlay();
            yield return null;

            Assert.AreEqual(AppScreen.Game, app.Current);
            Assert.IsNull(app.Tutorial);
        }

        [UnityTest]
        public IEnumerator Home_mid_tutorial_starts_it_over_next_time()
        {
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var tutorial = app.Tutorial!;
            yield return PlayGuidedMove(tutorial);
            Assert.AreEqual("Fill a row to clear it", tutorial.Hint!.Text);

            app.Back();   // the back button pauses the tutorial like any game
            Assert.IsTrue(tutorial.IsPaused);
            tutorial.Pause.PressHome();
            yield return null;
            Assert.AreEqual(AppScreen.Home, app.Current);
            Assert.AreEqual("Play", app.Home.PlayLabel);

            app.Home.PressPlay();
            yield return null;
            Assert.AreEqual(AppScreen.Tutorial, app.Current);
            Assert.AreEqual("Drag the piece onto the board", app.Tutorial!.Hint!.Text);
        }

        [UnityTest]
        public IEnumerator Daily_deals_todays_puzzle_and_home_offers_to_continue_it()
        {
            var app = Boot();
            yield return null;
            Assert.AreEqual("Daily", app.Home.DailyLabel);
            Assert.IsTrue(app.Home.DailyEnabled);

            app.Home.PressDaily();
            yield return null;
            Assert.AreEqual(AppScreen.Daily, app.Current);
            var daily = app.Daily!;
            Assert.AreEqual("c3d,t4a,c3a", TestGames.TrayIds(daily.Game), "the golden first tray, the same on every phone");
            yield return DragAndRelease(daily, 0, PointerFor(daily.Layout, daily.Game.GetTrayPiece(0)!, 0, 0));
            Assert.AreEqual(1, daily.Game.Placements);

            daily.TogglePause();
            daily.Pause.PressHome();
            yield return null;
            Assert.AreEqual("Daily · continue", app.Home.DailyLabel);
            Assert.AreEqual("Play", app.Home.PlayLabel, "the Daily is not a Classic game");

            app.Home.PressDaily();
            yield return null;
            Assert.AreEqual(1, app.Daily!.Game.Placements);
        }

        [UnityTest]
        public IEnumerator Finishing_the_daily_extends_the_streak_and_offers_share()
        {
            SaveDaily(new DailyRecord { LastDay = Day - 1, Streak = 2, BestStreak = 2 },
                TestGames.Position(TestBoards.OneMoveFromGameOver, "i2h", "o3", "o3"));
            var app = Boot();
            yield return null;
            Assert.AreEqual("Daily · continue", app.Home.DailyLabel);
            app.Home.PressDaily();
            yield return null;
            var daily = app.Daily!;

            yield return DragAndRelease(daily, 0, PointerFor(daily.Layout, PieceCatalog.Standard["i2h"], 0, 1));

            Assert.IsTrue(daily.GameOver.IsShown);
            Assert.IsFalse(daily.GameOver.OffersPlayAgain, "one scored attempt a day");
            StringAssert.Contains("3-day streak", daily.GameOver.ScoresText);
            daily.GameOver.PressShare();
            Assert.AreEqual(1, _shared.Count);
            StringAssert.Contains("Daily 2026-10-20", _shared[0]);
            StringAssert.Contains("3-day streak", _shared[0]);

            daily.GameOver.PressHome();
            yield return null;
            Assert.AreEqual("Daily · done", app.Home.DailyLabel);
            Assert.IsFalse(app.Home.DailyEnabled);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.AreEqual(Day, saved.Daily.Record.LastDay);
            Assert.AreEqual(3, saved.Daily.Record.Streak);
            Assert.IsFalse(saved.Daily.HasGame);
            Assert.AreEqual(0, saved.BestScore, "the Daily's score isn't Classic's best");
            Assert.AreEqual(0, saved.NextGameIndex, "the Daily doesn't count as a Classic game");
        }

        [UnityTest]
        public IEnumerator A_played_daily_waits_for_the_next_day()
        {
            SaveDaily(new DailyRecord { LastDay = Day, Streak = 1, BestStreak = 1 });
            var app = Boot();
            yield return null;
            Assert.AreEqual("Daily · done", app.Home.DailyLabel);
            app.Home.PressDaily();
            Assert.AreEqual(AppScreen.Home, app.Current);

            _now = October20.AddDays(1);   // midnight passes with Home on screen, or the app comes back on a later day
            yield return null;
            Assert.AreEqual("Daily · 1-day streak", app.Home.DailyLabel);
            Assert.IsTrue(app.Home.DailyEnabled);
            app.Home.PressDaily();
            yield return null;

            Assert.AreEqual(AppScreen.Daily, app.Current);
            Assert.AreEqual(TestGames.TrayIds(DailyPuzzle.NewGame(Day + 1)), TestGames.TrayIds(app.Daily!.Game));
        }

        [UnityTest]
        public IEnumerator Setting_the_clock_back_cannot_replay_a_day()
        {
            SaveDaily(new DailyRecord { LastDay = Day, Streak = 1, BestStreak = 1 });
            _now = October20.AddDays(-3);
            var app = Boot();
            yield return null;

            Assert.AreEqual("Daily · done", app.Home.DailyLabel);
            Assert.IsFalse(app.Home.DailyEnabled);
            app.PlayDaily();
            Assert.AreEqual(AppScreen.Home, app.Current);
            Assert.IsNull(app.Daily);
        }

        [UnityTest]
        public IEnumerator An_attempt_left_from_yesterday_is_dropped()
        {
            SaveDaily(new DailyRecord(), TestGames.Position(Board.Empty, "dot", "o2", "i3h"), attemptDay: Day - 1);
            var app = Boot();
            yield return null;
            Assert.AreEqual("Daily", app.Home.DailyLabel, "yesterday's attempt is not today's to continue");

            app.Home.PressDaily();
            yield return null;

            Assert.AreEqual("c3d,t4a,c3a", TestGames.TrayIds(app.Daily!.Game));
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.AreEqual(Day, saved.Daily.GameDay);
            Assert.AreEqual(-1, saved.Daily.Record.LastDay, "the dropped attempt scored nothing");
        }

        [UnityTest]
        public IEnumerator Classic_and_daily_keep_separate_games()
        {
            MarkTutorialDone();
            var app = Boot();
            yield return null;
            app.Home.PressPlay();
            yield return null;
            var classic = app.Game!;
            yield return DragAndRelease(classic, 0, PointerFor(classic.Layout, classic.Game.GetTrayPiece(0)!, 0, 0));
            string classicTray = TestGames.TrayIds(classic.Game);
            classic.TogglePause();
            classic.Pause.PressHome();
            yield return null;

            app.Home.PressDaily();
            yield return null;
            var daily = app.Daily!;
            yield return DragAndRelease(daily, 0, PointerFor(daily.Layout, daily.Game.GetTrayPiece(0)!, 0, 0));
            daily.TogglePause();
            daily.Pause.PressHome();
            yield return null;

            app.Home.PressPlay();
            yield return null;
            Assert.AreSame(classic, app.Game);
            Assert.AreEqual(1, classic.Game.Placements);
            Assert.AreEqual(classicTray, TestGames.TrayIds(classic.Game));
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.AreEqual(1, saved.Game.Placements);
            Assert.AreEqual(1, saved.Daily.Game.Placements);
            Assert.AreEqual(classic.Game.Score, saved.BestScore, "the Daily's score isn't Classic's best");
            Assert.AreEqual(0, saved.NextGameIndex);
        }
    }
}
