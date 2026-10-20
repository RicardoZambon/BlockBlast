namespace BlockPuzzle.Core.Tests;

public class BoardTests
{
    [Fact]
    public void Parse_and_ToString_round_trip()
    {
        string[] rows =
        {
            "#.......",
            ".#......",
            "..#.....",
            "...#....",
            "....#...",
            ".....#..",
            "......#.",
            ".......#",
        };

        var board = Board.Parse(rows);

        Assert.Equal(string.Join("\n", rows), board.ToString());
        Assert.True(board.IsFilled(0, 0));
        Assert.False(board.IsFilled(0, 1));
        Assert.True(board.IsFilled(7, 7));
    }

    [Fact]
    public void Parse_rejects_bad_input()
    {
        Assert.Throws<ArgumentException>(() => Board.Parse("........"));
        Assert.Throws<ArgumentException>(() => Board.Parse(
            "........", "........", "........", "........", "........", "........", "........", "......."));
        Assert.Throws<ArgumentException>(() => Board.Parse(
            "x.......", "........", "........", "........", "........", "........", "........", "........"));
    }

    [Fact]
    public void Fits_is_false_only_where_cells_overlap()
    {
        var board = new Board(TestBoards.Cell(0, 0));

        Assert.False(board.Fits(TestBoards.Cell(0, 0)));
        Assert.True(board.Fits(TestBoards.Cell(0, 1)));
    }

    [Fact]
    public void Placing_without_full_lines_only_adds_cells()
    {
        ulong piece = TestBoards.Cell(0, 0) | TestBoards.Cell(0, 1);

        var result = Board.Empty.Place(piece);

        Assert.Equal(piece, result.Board.Cells);
        Assert.Equal(0, result.Lines);
        Assert.Equal(0UL, result.ClearedMask);
        Assert.False(result.BoardCleared);
    }

    [Fact]
    public void Placing_on_filled_cells_throws()
    {
        var board = new Board(TestBoards.Cell(3, 3));

        Assert.Throws<InvalidOperationException>(() => board.Place(TestBoards.Cell(3, 3)));
    }

    [Fact]
    public void Completing_a_row_clears_it()
    {
        var board = Board.Parse(
            "#######.",
            "#.......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");

        var result = board.Place(TestBoards.Cell(0, 7));

        Assert.Equal(1, result.Lines);
        Assert.Equal(0xFFUL, result.ClearedMask);
        Assert.Equal(new Board(TestBoards.Cell(1, 0)), result.Board);
        Assert.False(result.BoardCleared);
    }

    [Fact]
    public void Completing_a_column_clears_it()
    {
        var board = Board.Parse(
            "...#....",
            "...#....",
            "...#....",
            "........",
            "...#....",
            "...#....",
            "...#...#",
            "...#....");

        var result = board.Place(TestBoards.Cell(3, 3));

        Assert.Equal(1, result.Lines);
        Assert.Equal(0x0101010101010101UL << 3, result.ClearedMask);
        Assert.Equal(new Board(TestBoards.Cell(6, 7)), result.Board);
    }

    [Fact]
    public void Completing_a_row_and_a_column_at_once_clears_both()
    {
        var board = Board.Parse(
            ".....#..",
            ".....#..",
            "#####.##",
            ".....#..",
            ".....#..",
            ".....#..",
            ".....#..",
            "#....#..");

        var result = board.Place(TestBoards.Cell(2, 5));

        Assert.Equal(2, result.Lines);
        Assert.Equal(new Board(TestBoards.Cell(7, 0)), result.Board);
    }

    [Fact]
    public void HasFullLine_detects_a_full_row_or_column()
    {
        Assert.False(TestBoards.Checkerboard.HasFullLine);
        Assert.True(new Board(0xFFUL).HasFullLine);                        // row 0
        Assert.True(new Board(0x0101010101010101UL << 5).HasFullLine);     // column 5
    }

    [Fact]
    public void Clearing_every_filled_cell_reports_a_board_clear()
    {
        var board = Board.Parse(
            "........",
            "........",
            "........",
            "#######.",
            "........",
            "........",
            "........",
            "........");

        var result = board.Place(TestBoards.Cell(3, 7));

        Assert.True(result.Board.IsEmpty);
        Assert.True(result.BoardCleared);
    }
}
