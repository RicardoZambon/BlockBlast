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
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        GameController Boot()
        {
            _controller = new GameObject("Game").AddComponent<GameController>();
            _controller.Boot(SavePath);
            return _controller;
        }

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

        [UnityTest]
        public IEnumerator A_tap_without_dragging_places_nothing()
        {
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
            // From GameSessionTests.A_placement_can_end_the_game: placing "i2h" at (0, 1) leaves no room for the two 3x3 squares.
            var board = Board.Parse(
                "#...#.#.",
                ".#.#.#.#",
                "#.#.#.#.",
                ".#.#.#.#",
                "#.#.#.#.",
                ".#.#.#.#",
                "#.#.#.#.",
                ".#.#.#.#");
            var snapshot = TestGames.Position(board, "i2h", "o3", "o3").ToSnapshot();
            snapshot.GameIndex = 4;
            snapshot.RecentQuickLosses = 1;
            new SaveStore(SavePath).Write(new SaveData { HasGame = true, Game = snapshot, NextGameIndex = 4, NextRecentQuickLosses = 1 });

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
    }
}
