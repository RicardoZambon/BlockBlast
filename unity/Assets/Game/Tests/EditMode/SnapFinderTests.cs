#nullable enable
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace BlockPuzzle.Game.Tests
{
    public class SnapFinderTests
    {
        // Slot 0 holds the piece under test; the "dot" in slot 1 keeps crowded boards from being game over.
        static GameSession Game(Board board, string piece) => TestGames.Position(board, piece, "dot", "o3");

        static (int Row, int Col)? Snap(GameSession game, float row, float col) =>
            SnapFinder.TryFind(game, 0, row, col, out int r, out int c) ? (r, c) : ((int, int)?)null;

        [Test]
        public void An_exact_legal_position_snaps_to_itself()
        {
            Assert.AreEqual((2, 3), Snap(Game(Board.Empty, "o2"), 2.0f, 3.0f));
        }

        [Test]
        public void A_nearby_position_snaps_to_the_closest_cell()
        {
            var game = Game(Board.Empty, "o2");

            Assert.AreEqual((2, 3), Snap(game, 2.4f, 3.3f));
            Assert.AreEqual((3, 3), Snap(game, 2.6f, 3.3f));
        }

        [Test]
        public void A_blocked_spot_snaps_to_a_legal_neighbour()
        {
            var game = Game(new Board(TestBoards.Cell(2, 3)), "dot");

            Assert.AreEqual((2, 4), Snap(game, 2.2f, 3.4f));
        }

        [Test]
        public void Nothing_legal_within_one_cell_means_no_target()
        {
            var game = Game(TestBoards.Checkerboard, "i2h");
            Assert.IsFalse(game.IsGameOver);

            Assert.IsNull(Snap(game, 3.0f, 3.0f));
        }

        [Test]
        public void Ties_go_to_the_upper_left_candidate()
        {
            var game = Game(Board.Empty, "dot");

            Assert.AreEqual((2, 3), Snap(game, 2.5f, 3.0f));   // (2, 3) and (3, 3) are both 0.5 away
            Assert.AreEqual((2, 3), Snap(game, 2.0f, 3.5f));   // (2, 3) and (2, 4) are both 0.5 away
        }

        [Test]
        public void A_piece_over_the_tray_has_no_target()
        {
            Assert.IsNull(Snap(Game(Board.Empty, "o2"), 10.0f, 3.0f));
        }

        [Test]
        public void TryFind_allocates_nothing()
        {
            var game = Game(Board.Empty, "o2");
            SnapFinder.TryFind(game, 0, 2.4f, 3.3f, out _, out _);

            Assert.That(() => { SnapFinder.TryFind(game, 0, 2.4f, 3.3f, out _, out _); }, Is.Not.AllocatingGCMemory());
        }
    }
}
