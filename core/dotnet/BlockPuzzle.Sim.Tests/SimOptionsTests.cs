namespace BlockPuzzle.Sim.Tests;

public class SimOptionsTests
{
    static SimOptions Parse(params string[] args) => SimOptions.Parse(args)!;

    [Theory]
    [InlineData(10, 50, 1)]
    [InlineData(120, 50, 3)]
    [InlineData(20000, 30, 667)]
    [InlineData(20000, 50, 400)]
    public void There_are_enough_players_for_every_requested_game(int games, int gamesPerPlayer, int players)
    {
        var options = Parse("--games", games.ToString(), "--games-per-player", gamesPerPlayer.ToString());

        Assert.Equal(games, options.Games);
        Assert.Equal(players, options.Players);
    }

    [Fact]
    public void Check_target_is_an_opt_in_flag()
    {
        Assert.True(Parse("--check-target", "--profile", "baseline").CheckTarget);
        Assert.False(Parse().CheckTarget);
    }

    [Theory]
    [InlineData("--bot", "random")]
    [InlineData("--profile", "hard")]
    public void Check_target_needs_the_greedy_bot_and_the_baseline_profile(string option, string value)
    {
        Assert.Throws<ArgumentException>(() => SimOptions.Parse(new[] { "--check-target", option, value }));
    }
}
