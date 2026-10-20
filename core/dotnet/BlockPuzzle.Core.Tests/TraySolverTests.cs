namespace BlockPuzzle.Core.Tests;

public class TraySolverTests
{
    static PieceShape Shape(string id) => PieceCatalog.Standard[id];

    [Fact]
    public void Three_big_squares_fit_on_an_empty_board()
    {
        Assert.True(TraySolver.CanPlaceAll(Board.Empty, new[] { Shape("o3"), Shape("o3"), Shape("o3") }));
    }

    [Fact]
    public void A_piece_that_fits_nowhere_fails()
    {
        Assert.False(TraySolver.CanPlaceAll(TestBoards.Checkerboard, new[] { Shape("dot"), Shape("i2h") }));
    }

    [Fact]
    public void Order_matters_because_clears_free_space()
    {
        // Only small pieces fit at first. The dot completes row 0, which then has room for the 5-long line.
        var board = Board.Parse(
            "#######.",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.");

        Assert.False(Shape("i5h").FitsAnywhere(board));
        Assert.True(TraySolver.CanPlaceAll(board, new[] { Shape("i5h"), Shape("dot") }));
        Assert.False(TraySolver.CanPlaceAll(board, new[] { Shape("i5h"), Shape("i3h") }));
    }

    [Fact]
    public void Used_slots_are_skipped()
    {
        Assert.True(TraySolver.CanPlaceAll(TestBoards.Checkerboard, new PieceShape?[] { null, Shape("dot"), null }));
    }
}
