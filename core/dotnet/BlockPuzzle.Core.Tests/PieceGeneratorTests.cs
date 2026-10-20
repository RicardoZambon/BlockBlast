namespace BlockPuzzle.Core.Tests;

public class PieceGeneratorTests
{
    static PieceGenerator Create(GeneratorSettings settings, ulong seed = 1) =>
        new(PieceCatalog.Standard, settings, new Pcg32(seed));

    static string Ids(PieceShape[] tray) => string.Join(",", tray.Select(p => p.Id));

    static readonly Board AlmostFullRow = Board.Parse(
        "######..",
        "........",
        "........",
        "........",
        "........",
        "........",
        "........",
        "........");

    [Fact]
    public void Trays_have_three_pieces()
    {
        var tray = Create(GeneratorSettings.Baseline()).Deal(Board.Empty, placements: 0, mercyGame: false);

        Assert.Equal(PieceGenerator.TraySize, tray.Length);
        Assert.All(tray, p => Assert.NotNull(p));
    }

    [Fact]
    public void The_same_seed_deals_the_same_trays()
    {
        var a = Create(GeneratorSettings.Baseline(), seed: 7);
        var b = Create(GeneratorSettings.Baseline(), seed: 7);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(Ids(a.Deal(AlmostFullRow, i * 3, mercyGame: false)), Ids(b.Deal(AlmostFullRow, i * 3, mercyGame: false)));
        }
    }

    [Fact]
    public void Different_seeds_deal_different_trays()
    {
        var a = Create(GeneratorSettings.Baseline(), seed: 1);
        var b = Create(GeneratorSettings.Baseline(), seed: 2);

        var first = Enumerable.Range(0, 20).Select(_ => Ids(a.Deal(Board.Empty, 0, false))).ToArray();
        var second = Enumerable.Range(0, 20).Select(_ => Ids(b.Deal(Board.Empty, 0, false))).ToArray();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void At_least_one_piece_fits_even_on_a_crowded_board()
    {
        var generator = Create(GeneratorSettings.Baseline());

        for (int i = 0; i < 200; i++)
        {
            var tray = generator.Deal(TestBoards.Checkerboard, placements: 50, mercyGame: false);
            Assert.Contains(tray, p => p.FitsAnywhere(TestBoards.Checkerboard));
        }
    }

    [Fact]
    public void Whole_tray_deals_can_be_placed()
    {
        var generator = Create(GeneratorSettings.Gentle());
        var board = Board.Parse(
            "####....",
            "####....",
            "####....",
            "####....",
            "........",
            "........",
            "........",
            "........");

        for (int i = 0; i < 200; i++)
        {
            Assert.True(TraySolver.CanPlaceAll(board, generator.Deal(board, placements: 10, mercyGame: false)));
        }
    }

    [Fact]
    public void Mercy_deals_smaller_pieces()
    {
        double AverageCells(bool mercyGame)
        {
            var generator = Create(GeneratorSettings.Baseline(), seed: 3);
            return Enumerable.Range(0, 2000)
                .SelectMany(_ => generator.Deal(Board.Empty, 0, mercyGame))
                .Average(p => p.CellCount);
        }

        Assert.True(AverageCells(mercyGame: true) < AverageCells(mercyGame: false) - 0.3);
    }

    [Fact]
    public void The_helpful_bias_prefers_pieces_that_complete_a_line()
    {
        double ClearingShare(int chancePercent)
        {
            var settings = GeneratorSettings.Baseline();
            settings.HelpfulChancePercent = chancePercent;
            var generator = Create(settings, seed: 5);
            var pieces = Enumerable.Range(0, 1000).SelectMany(_ => generator.Deal(AlmostFullRow, 0, false)).ToArray();
            return pieces.Count(p => p.CanClearLine(AlmostFullRow)) / (double)pieces.Length;
        }

        Assert.True(ClearingShare(100) > ClearingShare(0) + 0.3);
    }

    [Fact]
    public void The_rescue_tray_always_fits()
    {
        var generator = Create(GeneratorSettings.Baseline());

        Assert.True(TraySolver.CanPlaceAll(TestBoards.Checkerboard, generator.DealRescue(TestBoards.Checkerboard)));
        Assert.True(TraySolver.CanPlaceAll(AlmostFullRow, generator.DealRescue(AlmostFullRow)));
    }
}
