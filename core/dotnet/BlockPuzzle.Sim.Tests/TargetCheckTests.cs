namespace BlockPuzzle.Sim.Tests;

public class TargetCheckTests
{
    static GameResult[] Games(params int[] placements) =>
        placements.Select(p => new GameResult(p, Score: 0, Lines: 0, MaxStreak: 0, Mercy: false, Capped: false)).ToArray();

    [Fact]
    public void Results_inside_the_target_pass()
    {
        Assert.Null(TargetCheck.Miss(Games(90, 100, 110)));
    }

    [Theory]
    [InlineData(60)]
    [InlineData(200)]
    public void A_median_outside_80_to_150_misses(int median)
    {
        Assert.Contains("median", TargetCheck.Miss(Games(median, median, median)));
    }

    [Fact]
    public void Too_many_games_ending_before_placement_20_miss()
    {
        // 2 of 20 games (10%) end before placement 20, while the median is fine.
        var results = Games(Enumerable.Repeat(100, 18).Concat(new[] { 10, 15 }).ToArray());

        Assert.Contains("before placement 20", TargetCheck.Miss(results));
    }
}
