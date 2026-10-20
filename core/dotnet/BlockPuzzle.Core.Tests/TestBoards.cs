namespace BlockPuzzle.Core.Tests;

static class TestBoards
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

    public static ulong Cell(int row, int col) => 1UL << Board.BitIndex(row, col);
}
