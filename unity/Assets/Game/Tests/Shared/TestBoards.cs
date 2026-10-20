#nullable enable
using BlockPuzzle.Core;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>The engine tests' crowded boards (core/dotnet/BlockPuzzle.Core.Tests).</summary>
    public static class TestBoards
    {
        /// <summary>No two empty cells touch, so only the single-cell piece fits.</summary>
        public static readonly Board Checkerboard = Board.Parse(
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#");

        /// <summary>Four 3x3 blocks with rows 3 and 7 and columns 3 and 7 empty: only lines and small pieces fit.</summary>
        public static readonly Board Crossroads = Board.Parse(
            "###.###.",
            "###.###.",
            "###.###.",
            "........",
            "###.###.",
            "###.###.",
            "###.###.",
            "........");

        /// <summary>Row 0 needs two more cells to clear.</summary>
        public static readonly Board AlmostFullRow = Board.Parse(
            "######..",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");

        public static ulong Cell(int row, int col) => 1UL << Board.BitIndex(row, col);
    }
}
