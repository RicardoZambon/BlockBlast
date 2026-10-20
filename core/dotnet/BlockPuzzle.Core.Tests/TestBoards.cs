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

    public static ulong Cell(int row, int col) => 1UL << Board.BitIndex(row, col);
}
