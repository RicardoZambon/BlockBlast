using Xunit.Abstractions;

namespace BlockPuzzle.Core.Tests;

/// <summary>
/// Golden values: a seed must deal the same pieces on every platform and every app version, because
/// the Daily Puzzle and resumed games depend on it. If one of these fails, dealing changed. Update the
/// values only on purpose; the failing test's output shows the new ones.
/// </summary>
public class DeterminismTests
{
    const ulong Seed = 20261020;
    readonly ITestOutputHelper _output;

    public DeterminismTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("gentle", "i2h,i2h,t4a c3c,i2v,i3v c3b,t4d,c3a i3h,i2h,i3v s4v,o3,l4d j4b,c5c,l4d j4a,l4d,l4c c3d,i3h,z4v")]
    [InlineData("baseline", "i2h,i2h,t4a o2,i3v,i2v i4h,o2,t4b l4a,j4a,i3h i4v,o3,c3c c3c,i5h,i3h i2v,c3d,s4h i3h,i4h,c3a")]
    [InlineData("hard", "i2v,l4b,i2h dot,j4a,c3c r23,s4h,t4b c3b,o3,j4b i3h,t4b,i3v o3,o3,c5a j4b,c5c,i5h c5c,c5a,c5a")]
    public void A_seed_always_deals_the_same_trays(string profile, string expected)
    {
        var generator = new PieceGenerator(PieceCatalog.Standard, GeneratorSettings.ForProfile(profile), new Pcg32(Seed));

        string actual = string.Join(" ", Enumerable.Range(0, 8).Select(i =>
            string.Join(",", generator.Deal(Board.Empty, placements: i * 15, mercyGame: i % 2 == 1).Select(p => p.Id))));

        _output.WriteLine(actual);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("gentle", "i2h,i2h,t4a t4b,t4d,i3v c3b,t4d,j4b i3h,i2h,i3v j4b,c5c,l4d j4a,l4d,l4c c3d,i3h,z4v c5c,j4b,c3c c5c,dot,j4c dot,c5c,c5a l4c,i4h,dot z4h,i2h,dot c3c,c3b,i2v")]
    [InlineData("baseline", "i2h,i2h,t4a o2,i3v,t4d i4h,o2,t4b l4a,j4a,i3h i4v,o3,c3c c3c,i5h,i3h i2v,c3d,s4h i3h,i4h,c3a dot,i5v,t4b i2h,dot,s4v dot,c5b,j4a i3v,c3c,c3b i4v,c3a,i2v")]
    [InlineData("hard", "i2v,l4b,i2h i3v,c5b,t4b r23,s4h,t4b c3b,o3,j4b i3h,t4b,i3v o3,o3,c5a j4b,c5c,i5h c5c,c5a,c5a c3d,dot,z4v i5h,dot,c5c dot,l4a,c3b c3b,i4v,c3a i2v,c3c,c3c")]
    public void A_seed_always_deals_the_same_trays_on_a_crowded_board(string profile, string expected)
    {
        // Few pieces fit on these boards, so this reaches what an empty board never does: WholeTray
        // re-deals, the AtLeastOne swap (only single cells fit the checkerboard) and the revive's rescue tray.
        var generator = new PieceGenerator(PieceCatalog.Standard, GeneratorSettings.ForProfile(profile), new Pcg32(Seed));

        var trays = Enumerable.Range(0, 8).Select(i => generator.Deal(TestBoards.Crossroads, placements: 10 + i * 15, mercyGame: false))
            .Concat(Enumerable.Range(0, 3).Select(i => generator.Deal(TestBoards.Checkerboard, placements: 50 + i, mercyGame: false)))
            .Concat(Enumerable.Range(0, 2).Select(_ => generator.DealRescue(TestBoards.Crossroads)));
        string actual = string.Join(" ", trays.Select(tray => string.Join(",", tray.Select(p => p.Id))));

        _output.WriteLine(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void A_seeded_game_played_first_fit_always_ends_the_same_way()
    {
        var game = new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), Seed);
        for (int i = 0; i < 10_000 && !game.IsGameOver; i++) TestPlay.FirstFit(game);

        string actual = $"placements={game.Placements} score={game.Score} lines={game.LinesCleared} board={game.Board.Cells:X16}";

        _output.WriteLine(actual);
        Assert.Equal("placements=43 score=459 lines=15 board=22663EFEC7D03F24", actual);
    }
}
