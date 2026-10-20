namespace BlockPuzzle.Core.Tests;

public class PieceGeneratorTests
{
    static PieceGenerator Create(GeneratorSettings settings, ulong seed = 1) =>
        new(PieceCatalog.Standard, settings, new Pcg32(seed));

    static string Ids(IEnumerable<PieceShape?> tray) => string.Join(",", tray.Select(p => p?.Id ?? "-"));

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
    public void Whole_tray_deals_can_be_placed_where_random_trays_often_cannot()
    {
        var unguarded = GeneratorSettings.Gentle();
        unguarded.FitGuarantee = FitGuarantee.None;
        var random = Create(unguarded);
        var gentle = Create(GeneratorSettings.Gentle());

        // Without the guarantee this board regularly gets trays that can't all be placed...
        var randomTrays = Enumerable.Range(0, 200).Select(_ => random.Deal(TestBoards.Crossroads, 10, false)).ToArray();
        Assert.Contains(randomTrays, tray => !TraySolver.CanPlaceAll(TestBoards.Crossroads, tray));

        // ...and with it, every one of these deals fits.
        for (int i = 0; i < 200; i++)
        {
            Assert.True(TraySolver.CanPlaceAll(TestBoards.Crossroads, gentle.Deal(TestBoards.Crossroads, placements: 10, mercyGame: false)));
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

    [Theory]
    [InlineData("gentle")]
    [InlineData("baseline")]
    [InlineData("hard")]
    public void Dealing_into_a_tray_deals_the_same_pieces_as_Deal(string profile)
    {
        // Same seed, so the two generators stay in step as long as they make the same random calls.
        var settings = GeneratorSettings.ForProfile(profile);
        var returning = Create(settings, seed: 3);
        var filling = Create(settings, seed: 3);
        var tray = new PieceShape?[PieceGenerator.TraySize];

        foreach (var board in new[] { Board.Empty, TestBoards.Crossroads, TestBoards.Checkerboard, AlmostFullRow })
        {
            for (int placements = 0; placements < 60; placements += 15)
            {
                filling.Deal(board, placements, mercyGame: placements == 15, tray);
                Assert.Equal(Ids(returning.Deal(board, placements, mercyGame: placements == 15)), Ids(tray));
            }
            filling.DealRescue(board, tray);
            Assert.Equal(Ids(returning.DealRescue(board)), Ids(tray));
        }
    }

    [Fact]
    public void Dealing_into_a_tray_allocates_nothing_from_a_new_generators_first_deal()
    {
        // Every game builds a new generator, so its very first deals must not allocate either.
        // Gentle's WholeTray re-deals are the most work per deal.
        var tray = new PieceShape?[PieceGenerator.TraySize];
        DealEveryKind(Create(GeneratorSettings.Gentle(), seed: 5), tray);   // compiles the code and sets up statics
        var generator = Create(GeneratorSettings.Gentle(), seed: 5);

        long before = GC.GetAllocatedBytesForCurrentThread();
        DealEveryKind(generator, tray);

        Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    static void DealEveryKind(PieceGenerator generator, PieceShape?[] tray)
    {
        for (int i = 0; i < 20; i++)
        {
            generator.Deal(AlmostFullRow, placements: i, mercyGame: false, tray);   // helpful picks that can clear a line
            generator.Deal(TestBoards.Crossroads, placements: i * 5, mercyGame: i % 2 == 1, tray);
            generator.Deal(TestBoards.Checkerboard, placements: i * 5, mercyGame: false, tray);   // only a single cell fits
            generator.DealRescue(TestBoards.Crossroads, tray);
            generator.DealRescue(TestBoards.Checkerboard, tray);
        }
    }

    [Fact]
    public void Dealing_into_a_missing_or_wrong_size_tray_throws()
    {
        var generator = Create(GeneratorSettings.Baseline());

        Assert.Throws<ArgumentNullException>(() => generator.Deal(Board.Empty, 0, mercyGame: false, null!));
        Assert.Throws<ArgumentException>(() => generator.Deal(Board.Empty, 0, mercyGame: false, new PieceShape?[2]));
        Assert.Throws<ArgumentException>(() => generator.DealRescue(Board.Empty, new PieceShape?[4]));
    }
}
