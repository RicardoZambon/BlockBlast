#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>A whole game through GameController, with pointer positions taken from its layout.</summary>
    public class GameFlowTests
    {
        string _folder = "";
        GameObject? _camera;
        GameController? _controller;

        string SavePath => Path.Combine(_folder, "save.json");

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "blockpuzzle-playtests-" + System.Guid.NewGuid().ToString("N"));
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
            if (_controller != null) Object.Destroy(_controller.gameObject);
            if (_camera != null) Object.Destroy(_camera);
            yield return null;
            if (!Directory.Exists(_folder)) yield break;
            foreach (string file in Directory.GetFiles(_folder, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(_folder, recursive: true);
        }

        GameController Boot()
        {
            _controller = new GameObject("Game").AddComponent<GameController>();
            _controller.Boot(SavePath);
            return _controller;
        }

        /// <summary>Saves a game in progress in the given position, so the next Boot resumes it instead of dealing at random.</summary>
        void SavePosition(GameSession position, int gameIndex = 0, int recentQuickLosses = 0, int nextGameIndex = 0)
        {
            var snapshot = position.ToSnapshot();
            snapshot.GameIndex = gameIndex;
            snapshot.RecentQuickLosses = recentQuickLosses;
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = snapshot, NextGameIndex = nextGameIndex });
        }

        // From GameSessionTests.A_placement_can_end_the_game: placing "i2h" at (0, 1) leaves no room for the two 3x3 squares.
        static readonly Board OneMoveFromGameOver = Board.Parse(
            "#...#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#");

        /// <summary>The pointer position whose dragged piece has its top-left corner exactly on (row, col).</summary>
        static Vector2 PointerFor(BoardLayout layout, PieceShape shape, int row, int col)
        {
            float cell = layout.CellSize;
            Vector2 topLeft = layout.BoardTopLeft + new Vector2(col * cell, -row * cell);
            return topLeft + new Vector2(shape.Width / 2f * cell, -(BoardLayout.LiftCells + shape.Height) * cell);
        }

        static IEnumerator DragAndRelease(GameController game, int slot, Vector2 to)
        {
            Assert.IsTrue(game.Drag.Press(game.Layout.TraySlotCenter(slot)));
            yield return null;
            game.Drag.Drag(to);
            yield return null;
            game.Drag.Release(to);
            yield return null;
        }

        static IEnumerator WaitForReturn()
        {
            yield return new WaitForSeconds(DragController.ReturnSeconds);
            yield return null;
            yield return null;
        }

        static string TrayIds(GameSession game) => string.Join(",", game.Tray.ToArray().Select(p => p?.Id ?? "-"));

        [UnityTest]
        public IEnumerator A_new_game_shows_three_pieces_and_no_score()
        {
            var game = Boot();
            yield return null;

            Assert.AreEqual(0, game.Game.Placements);
            for (int slot = 0; slot < 3; slot++) Assert.IsNotNull(game.Game.GetTrayPiece(slot));
            Assert.AreEqual("0", game.Hud.ScoreText);
        }

        [UnityTest]
        public IEnumerator Dragging_a_piece_onto_the_board_places_it_and_saves()
        {
            var game = Boot();
            yield return null;
            var shape = game.Game.GetTrayPiece(0)!;

            yield return DragAndRelease(game, 0, PointerFor(game.Layout, shape, 0, 0));

            Assert.AreEqual(1, game.Game.Placements);
            Assert.IsNull(game.Game.GetTrayPiece(0));
            Assert.Greater(game.Game.Score, 0);
            Assert.AreEqual(game.Game.Score.ToString(), game.Hud.ScoreText);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsTrue(saved.HasGame);
            Assert.AreEqual(1, saved.Game.Placements);
        }

        [UnityTest]
        public IEnumerator Releasing_off_the_board_returns_the_piece_and_changes_nothing()
        {
            var game = Boot();
            yield return null;
            var shape = game.Game.GetTrayPiece(0)!;
            Vector2 farBelow = game.Layout.TraySlotCenter(2) + Vector2.down * (6 * game.Layout.CellSize);

            yield return DragAndRelease(game, 0, farBelow);
            yield return WaitForReturn();

            Assert.IsFalse(game.Drag.IsReturning);
            Vector2 home = game.Layout.TrayPieceTopLeft(0, shape.Width, shape.Height);
            Vector3 position = game.Tray.Slot(0).transform.position;
            Assert.AreEqual(home.x, position.x, 1e-3f);
            Assert.AreEqual(home.y, position.y, 1e-3f);
            Assert.AreEqual(BoardLayout.TrayScale, game.Tray.Slot(0).transform.localScale.x, 1e-3f);
            Assert.AreEqual(0, game.Game.Placements);
        }

        // A single cell lifted from its tray slot overlaps the board's bottom row, so without the drag threshold
        // these two tests would snap it there.
        [UnityTest]
        public IEnumerator A_tap_without_dragging_places_nothing()
        {
            SavePosition(TestGames.Position(Board.Empty, "dot", "o2", "i3h"));
            var game = Boot();
            yield return null;
            Vector2 slot0 = game.Layout.TraySlotCenter(0);

            Assert.IsTrue(game.Drag.Press(slot0));
            yield return null;
            game.Drag.Release(slot0);
            yield return WaitForReturn();

            Assert.AreEqual(0, game.Game.Placements);
            Assert.IsNotNull(game.Game.GetTrayPiece(0));
        }

        [UnityTest]
        public IEnumerator Holding_still_after_a_press_shows_no_ghost()
        {
            SavePosition(TestGames.Position(Board.Empty, "dot", "o2", "i3h"));
            var game = Boot();
            yield return null;
            Vector2 slot0 = game.Layout.TraySlotCenter(0);

            Assert.IsTrue(game.Drag.Press(slot0));
            for (int i = 0; i < 5; i++)
            {
                game.Drag.Drag(slot0);
                yield return null;
            }

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Assert.AreNotEqual(Palette.Ghost, game.Board.CellColor(r, c));
                    Assert.AreNotEqual(Palette.Clearing, game.Board.CellColor(r, c));
                }
            }
            game.Drag.Release(slot0);
            yield return WaitForReturn();
            Assert.AreEqual(0, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator A_second_press_during_a_drag_changes_nothing()
        {
            var game = Boot();
            yield return null;
            var shape = game.Game.GetTrayPiece(0)!;

            Assert.IsTrue(game.Drag.Press(game.Layout.TraySlotCenter(0)));
            Assert.IsFalse(game.Drag.Press(game.Layout.TraySlotCenter(1)));
            Vector2 target = PointerFor(game.Layout, shape, 0, 0);
            game.Drag.Drag(target);
            yield return null;
            game.Drag.Release(target);
            yield return null;

            Assert.IsNull(game.Game.GetTrayPiece(0));
            Assert.IsNotNull(game.Game.GetTrayPiece(1));
        }

        [UnityTest]
        public IEnumerator Pausing_mid_drag_sends_the_piece_back()
        {
            var game = Boot();
            yield return null;
            var shape = game.Game.GetTrayPiece(0)!;
            game.Drag.Press(game.Layout.TraySlotCenter(0));
            game.Drag.Drag(PointerFor(game.Layout, shape, 0, 0));
            yield return null;

            game.gameObject.SendMessage("OnApplicationPause", true);

            Assert.IsFalse(game.Drag.IsDragging);
            yield return WaitForReturn();
            Assert.IsFalse(game.Drag.IsReturning);
            Assert.AreEqual(0, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator A_killed_game_resumes_where_it_was()
        {
            var first = Boot();
            yield return null;
            var shape = first.Game.GetTrayPiece(0)!;
            yield return DragAndRelease(first, 0, PointerFor(first.Layout, shape, 0, 0));
            var board = first.Game.Board;
            string tray = TrayIds(first.Game);
            int score = first.Game.Score;

            Object.DestroyImmediate(first.gameObject);   // no quit callbacks, like the system killing the app
            var second = Boot();
            yield return null;

            Assert.AreEqual(board, second.Game.Board);
            Assert.AreEqual(tray, TrayIds(second.Game));
            Assert.AreEqual(score, second.Game.Score);
        }

        [UnityTest]
        public IEnumerator Game_over_shows_the_panel_and_play_again_starts_the_next_game()
        {
            // The saved NextGameIndex (9) is stale on purpose: the next game must follow the finished game's own index (4).
            SavePosition(TestGames.Position(OneMoveFromGameOver, "i2h", "o3", "o3"), gameIndex: 4, recentQuickLosses: 1, nextGameIndex: 9);

            var game = Boot();
            yield return null;
            Assert.IsFalse(game.Game.IsGameOver);
            yield return DragAndRelease(game, 0, PointerFor(game.Layout, PieceCatalog.Standard["i2h"], 0, 1));

            Assert.IsTrue(game.Game.IsGameOver);
            Assert.IsTrue(game.GameOver.IsShown);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsFalse(saved.HasGame);
            Assert.AreEqual(5, saved.NextGameIndex);
            Assert.AreEqual(2, saved.NextRecentQuickLosses);

            game.GameOver.PressPlayAgain();
            yield return null;

            Assert.IsFalse(game.GameOver.IsShown);
            Assert.AreEqual(5, game.Game.GameIndex);
            Assert.AreEqual(2, game.Game.RecentQuickLosses);
            Assert.AreEqual(0, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator Dragging_allocates_nothing()
        {
            var game = Boot();
            yield return null;
            var shape = game.Game.GetTrayPiece(0)!;
            Vector2 a = PointerFor(game.Layout, shape, 2, 2);
            Vector2 b = PointerFor(game.Layout, shape, 4, 3);
            game.Drag.Press(game.Layout.TraySlotCenter(0));
            game.Drag.Drag(a);
            game.Drag.Drag(b);
            yield return null;

            Assert.That(() =>
            {
                game.Drag.Drag(a);
                game.Drag.Drag(b);
            }, Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator A_failed_save_does_not_stop_the_game()
        {
            Directory.CreateDirectory(SavePath);   // a folder where the save file goes, so every save fails
            var game = Boot();
            yield return null;
            for (int slot = 0; slot < 3; slot++) Assert.IsTrue(game.Tray.Slot(slot).gameObject.activeSelf);
            var shape = game.Game.GetTrayPiece(0)!;

            yield return DragAndRelease(game, 0, PointerFor(game.Layout, shape, 0, 0));

            Assert.AreEqual(1, game.Game.Placements);
            Assert.AreEqual(game.Game.Score.ToString(), game.Hud.ScoreText);
        }

        [UnityTest]
        public IEnumerator Game_over_shows_its_panel_even_when_saving_fails()
        {
            SavePosition(TestGames.Position(OneMoveFromGameOver, "i2h", "o3", "o3"));
            File.SetAttributes(SavePath, FileAttributes.ReadOnly);   // it still loads, but saving over it fails
            var game = Boot();
            yield return null;

            yield return DragAndRelease(game, 0, PointerFor(game.Layout, PieceCatalog.Standard["i2h"], 0, 1));

            Assert.IsTrue(game.Game.IsGameOver);
            Assert.IsTrue(game.GameOver.IsShown);
        }

        [UnityTest]
        public IEnumerator A_touch_the_system_cancels_sends_the_piece_back()
        {
            var game = Boot();
            yield return null;
            var input = game.GetComponent<PointerInput>();
            Vector2 target = PointerFor(game.Layout, game.Game.GetTrayPiece(0)!, 0, 0);

            input.Apply(pressedThisFrame: true, held: true, releasedThisFrame: false, cancelled: false, game.Layout.TraySlotCenter(0));
            yield return null;
            input.Apply(pressedThisFrame: false, held: true, releasedThisFrame: false, cancelled: false, target);
            yield return null;
            input.Apply(pressedThisFrame: false, held: false, releasedThisFrame: true, cancelled: true, target);
            yield return WaitForReturn();

            Assert.AreEqual(0, game.Game.Placements);
            Assert.IsNotNull(game.Game.GetTrayPiece(0));
        }

        [UnityTest]
        public IEnumerator A_released_touch_places_the_piece()
        {
            var game = Boot();
            yield return null;
            var input = game.GetComponent<PointerInput>();
            Vector2 target = PointerFor(game.Layout, game.Game.GetTrayPiece(0)!, 0, 0);

            input.Apply(pressedThisFrame: true, held: true, releasedThisFrame: false, cancelled: false, game.Layout.TraySlotCenter(0));
            yield return null;
            input.Apply(pressedThisFrame: false, held: true, releasedThisFrame: false, cancelled: false, target);
            yield return null;
            input.Apply(pressedThisFrame: false, held: false, releasedThisFrame: true, cancelled: false, target);
            yield return null;

            Assert.AreEqual(1, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator Losing_focus_mid_drag_sends_the_piece_back()
        {
            var game = Boot();
            yield return null;
            game.Drag.Press(game.Layout.TraySlotCenter(0));
            game.Drag.Drag(PointerFor(game.Layout, game.Game.GetTrayPiece(0)!, 0, 0));
            yield return null;

            game.gameObject.SendMessage("OnApplicationFocus", false);

            Assert.IsFalse(game.Drag.IsDragging);
            yield return WaitForReturn();
            Assert.AreEqual(0, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator A_save_with_negative_counters_starts_a_normal_new_game()
        {
            new SaveStore(SavePath).Write(new SaveData { NextGameIndex = -3, NextRecentQuickLosses = -1, BestScore = -5 });

            var game = Boot();
            yield return null;

            Assert.AreEqual(0, game.Game.GameIndex);
            Assert.AreEqual(0, game.Game.RecentQuickLosses);
            Assert.AreEqual(0, game.Game.Placements);
        }

        [UnityTest]
        public IEnumerator A_saved_game_that_is_already_over_shows_game_over()
        {
            SavePosition(TestGames.Position(TestBoards.Checkerboard, "i2h", "o3", "o3"), gameIndex: 2);

            var game = Boot();
            yield return null;

            Assert.IsTrue(game.Game.IsGameOver);
            Assert.IsTrue(game.GameOver.IsShown);
            Assert.IsTrue(new SaveStore(SavePath).TryRead(out var saved));
            Assert.IsFalse(saved.HasGame);
            Assert.AreEqual(3, saved.NextGameIndex);
        }

        [Test]
        public void Booting_without_a_main_camera_fails_clearly()
        {
            Object.DestroyImmediate(_camera);
            _camera = null;
            var controller = new GameObject("Game").AddComponent<GameController>();

            Assert.Throws<System.InvalidOperationException>(() => controller.Boot(SavePath));

            Object.DestroyImmediate(controller.gameObject);   // before its Start tries the default save
        }
    }
}
