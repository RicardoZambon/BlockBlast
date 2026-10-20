namespace BlockPuzzle.Sim.Tests;

public class TargetCheckTests
{
    static GameResult[] Games(params int[] placements) =>
        placements.Select(p => new GameResult(p, Score: 0, Lines: 0, MaxStreak: 0, Mercy: false, Capped: false)).ToArray();

    [Fact]
    public void The_target_names_a_real_profile_and_bot()
    {
        Assert.Contains(TargetCheck.Profile, GeneratorSettings.ProfileNames);
        Assert.Contains(TargetCheck.Bot, Bots.Names);
    }

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

    [Theory]
    [InlineData(80)]
    [InlineData(150)]
    public void A_median_of_exactly_80_or_150_passes(int median)
    {
        Assert.Null(TargetCheck.Miss(Games(median, median, median)));
    }

    [Fact]
    public void Exactly_5_percent_of_games_ending_early_misses()
    {
        // 1 of 20 games (5%) ends before placement 20; the target is under 5%.
        var results = Games(Enumerable.Repeat(100, 19).Concat(new[] { 10 }).ToArray());

        Assert.Contains("before placement 20", TargetCheck.Miss(results));
    }

    [Fact]
    public void Too_many_games_ending_before_placement_20_miss()
    {
        // 2 of 20 games (10%) end before placement 20, while the median is fine.
        var results = Games(Enumerable.Repeat(100, 18).Concat(new[] { 10, 15 }).ToArray());

        Assert.Contains("before placement 20", TargetCheck.Miss(results));
    }
}
