namespace BlockPuzzle.Core.Tests;

public class PieceCatalogTests
{
    [Fact]
    public void The_standard_catalog_has_37_unique_shapes()
    {
        var shapes = PieceCatalog.Standard.Shapes;

        Assert.Equal(37, shapes.Count);
        Assert.Equal(shapes.Count, shapes.Select(s => s.Id).Distinct().Count());
        Assert.Equal(shapes.Count, shapes.Select(s => s.BaseMask).Distinct().Count());
    }

    [Theory]
    [InlineData(PieceCategory.Tiny, 1)]
    [InlineData(PieceCategory.Small, 8)]
    [InlineData(PieceCategory.Medium, 19)]
    [InlineData(PieceCategory.Large, 9)]
    public void Each_category_has_the_expected_number_of_shapes(PieceCategory category, int expected)
    {
        Assert.Equal(expected, PieceCatalog.Standard.Shapes.Count(s => s.Category == category));
    }

    [Fact]
    public void Categories_match_cell_counts()
    {
        foreach (var shape in PieceCatalog.Standard.Shapes)
        {
            var expected = shape.CellCount switch
            {
                1 => PieceCategory.Tiny,
                2 or 3 => PieceCategory.Small,
                4 => PieceCategory.Medium,
                _ => PieceCategory.Large,
            };
            Assert.Equal(expected, shape.Category);
        }
    }

    [Fact]
    public void Shapes_can_be_looked_up_by_id()
    {
        Assert.Equal(9, PieceCatalog.Standard["o3"].CellCount);
        Assert.True(PieceCatalog.Standard.TryGet("i5h", out var line));
        Assert.Equal(5, line!.Width);
        Assert.False(PieceCatalog.Standard.TryGet("nope", out _));
        Assert.Throws<KeyNotFoundException>(() => PieceCatalog.Standard["nope"]);
    }

    [Fact]
    public void Duplicate_ids_are_rejected()
    {
        var shapes = new[]
        {
            PieceShape.FromRows("a", PieceCategory.Tiny, "#"),
            PieceShape.FromRows("a", PieceCategory.Small, "##"),
        };

        Assert.Throws<ArgumentException>(() => new PieceCatalog(shapes));
    }
}
