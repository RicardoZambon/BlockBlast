#nullable enable
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace BlockPuzzle.Game.Tests
{
    public class DragSessionTests
    {
        // Row 0 needs two cells; slot 0's "i2h" completes it at (0, 6).
        static GameSession AlmostFullRowGame() => TestGames.Position(TestBoards.AlmostFullRow, "i2h", "o3", "o3");

        static string TrayIds(GameSession game) => string.Join(",", game.Tray.ToArray().Select(p => p?.Id ?? "-"));

        [Test]
        public void A_drag_cannot_start_on_an_empty_or_missing_slot_or_after_game_over()
        {
            var withGap = TestGames.Position(Board.Empty, "i2h", "", "o3");
            Assert.IsFalse(new DragSession().TryBegin(withGap, 1));
            Assert.IsFalse(new DragSession().TryBegin(withGap, 3));
            Assert.IsFalse(new DragSession().TryBegin(withGap, -1));

            var over = TestGames.Position(TestBoards.Checkerboard, "i2h", "o3", "o3");
            Assert.IsTrue(over.IsGameOver);
            Assert.IsFalse(new DragSession().TryBegin(over, 0));
        }

        [Test]
        public void A_second_drag_cannot_start_while_one_is_running()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();

            Assert.IsTrue(drag.TryBegin(game, 0));
            Assert.IsFalse(drag.TryBegin(game, 1));
            Assert.AreEqual(0, drag.Slot);
        }

        [Test]
        public void Moving_over_a_legal_spot_sets_the_target_and_masks()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();
            drag.TryBegin(game, 0);

            drag.Move(game, 0.0f, 6.0f);

            Assert.IsTrue(drag.HasTarget);
            Assert.AreEqual((0, 6), (drag.TargetRow, drag.TargetCol));
            Assert.AreEqual(TestBoards.Cell(0, 6) | TestBoards.Cell(0, 7), drag.TargetMask);
            Assert.AreEqual(Board.RowMask(0), drag.ClearMask);
        }

        [Test]
        public void Moving_off_the_board_clears_the_target()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();
            drag.TryBegin(game, 0);
            drag.Move(game, 0.0f, 6.0f);

            drag.Move(game, 10.0f, 3.0f);

            Assert.IsFalse(drag.HasTarget);
            Assert.AreEqual(0UL, drag.TargetMask);
            Assert.AreEqual(0UL, drag.ClearMask);
        }

        [Test]
        public void Dropping_on_the_target_places_the_piece()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();
            drag.TryBegin(game, 0);
            drag.Move(game, 0.0f, 6.0f);

            Assert.IsTrue(drag.TryDrop(game, out var outcome));

            Assert.AreEqual(1, outcome.Lines);
            Assert.IsFalse(drag.IsDragging);
            Assert.AreEqual(-1, drag.Slot);
            Assert.IsNull(game.GetTrayPiece(0));
        }

        [Test]
        public void Dropping_without_a_target_changes_nothing()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();
            drag.TryBegin(game, 0);
            drag.Move(game, 10.0f, 3.0f);
            var board = game.Board;
            string tray = TrayIds(game);

            Assert.IsFalse(drag.TryDrop(game, out _));

            Assert.IsFalse(drag.IsDragging);
            Assert.AreEqual(board, game.Board);
            Assert.AreEqual(tray, TrayIds(game));
            Assert.AreEqual(0, game.Score);
        }

        [Test]
        public void Cancel_ends_the_drag_without_placing()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();
            drag.TryBegin(game, 0);
            drag.Move(game, 0.0f, 6.0f);

            drag.Cancel();

            Assert.IsFalse(drag.IsDragging);
            Assert.IsFalse(drag.HasTarget);
            Assert.AreEqual(0, game.Placements);
        }

        [Test]
        public void Move_allocates_nothing()
        {
            var game = AlmostFullRowGame();
            var drag = new DragSession();
            drag.TryBegin(game, 0);
            drag.Move(game, 0.0f, 6.0f);

            Assert.That(() =>
            {
                drag.Move(game, 0.2f, 5.8f);
                drag.Move(game, 4.0f, 2.0f);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
