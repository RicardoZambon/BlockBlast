using System.Numerics;

namespace BlockPuzzle.Core.Tests;

public class PieceShapeTests
{
    [Fact]
    public void FromRows_reads_size_and_cells()
    {
        var shape = PieceShape.FromRows("l4a", PieceCategory.Medium, "#.", "#.", "##");

        Assert.Equal(2, shape.Width);
        Assert.Equal(3, shape.Height);
        Assert.Equal(4, shape.CellCount);
        Assert.Equal(
            TestBoards.Cell(0, 0) | TestBoards.Cell(1, 0) | TestBoards.Cell(2, 0) | TestBoards.Cell(2, 1),
            shape.BaseMask);
    }

    [Theory]
    [InlineData(new[] { "#" }, 64)]
    [InlineData(new[] { "###", "###", "###" }, 36)]
    [InlineData(new[] { "#####" }, 32)]
    [InlineData(new[] { "#", "#", "#", "#", "#" }, 32)]
    public void Placement_count_is_every_position_inside_the_board(string[] rows, int expected)
    {
        var shape = PieceShape.FromRows("x", PieceCategory.Small, rows);

        Assert.Equal(expected, shape.PlacementCount);
    }

    [Fact]
    public void Each_placement_is_the_shape_moved_to_its_anchor()
    {
        var shape = PieceShape.FromRows("t4a", PieceCategory.Medium, "###", ".#.");

        for (int i = 0; i < shape.PlacementCount; i++)
        {
            int row = shape.AnchorRow(i);
            int col = shape.AnchorCol(i);
            Assert.True(row + shape.Height <= Board.Size);
            Assert.True(col + shape.Width <= Board.Size);
            Assert.True(shape.TryGetMask(row, col, out ulong mask));
            Assert.Equal(mask, shape.Placements[i]);
            Assert.Equal(shape.CellCount, BitOperations.PopCount(mask));
        }
    }

    [Fact]
    public void TryGetMask_rejects_positions_that_leave_the_board()
    {
        var shape = PieceShape.FromRows("i3h", PieceCategory.Small, "###");

        Assert.True(shape.TryGetMask(0, 5, out _));
        Assert.False(shape.TryGetMask(0, 6, out _));
        Assert.False(shape.TryGetMask(-1, 0, out _));
        Assert.False(shape.TryGetMask(8, 0, out _));
    }

    [Fact]
    public void TryGetMask_rejects_coordinates_that_would_overflow()
    {
        var square = PieceShape.FromRows("o3", PieceCategory.Large, "###", "###", "###");

        Assert.False(square.TryGetMask(0, int.MaxValue, out _));
        Assert.False(square.TryGetMask(int.MaxValue, 0, out _));
        Assert.False(square.TryGetMask(int.MaxValue - 1, 0, out _));
        Assert.False(square.TryGetMask(int.MinValue, 0, out _));
    }

    [Fact]
    public void FromRows_rejects_bad_art()
    {
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("a", PieceCategory.Small));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("b", PieceCategory.Small, "##", "#"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("c", PieceCategory.Small, "..", "##"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("d", PieceCategory.Small, "#.", "#."));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("f", PieceCategory.Small, "##", ".."));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("g", PieceCategory.Small, ".#", ".#"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("e", PieceCategory.Small, "#x"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("", PieceCategory.Small, "#"));
    }

    [Fact]
    public void FitsAnywhere_and_CanClearLine_check_every_position()
    {
        var board = Board.Parse(
            "######..",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");
        var domino = PieceShape.FromRows("i2h", PieceCategory.Small, "##");
        var tromino = PieceShape.FromRows("i3h", PieceCategory.Small, "###");

        Assert.True(domino.FitsAnywhere(board));
        Assert.True(domino.CanClearLine(board));
        Assert.False(tromino.CanClearLine(board));
        Assert.False(domino.FitsAnywhere(TestBoards.Checkerboard));
    }
}
