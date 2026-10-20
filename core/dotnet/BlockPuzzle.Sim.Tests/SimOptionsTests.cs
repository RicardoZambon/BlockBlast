namespace BlockPuzzle.Sim.Tests;

public class SimOptionsTests
{
    static SimOptions Parse(params string[] args) => SimOptions.Parse(args)!;

    public static TheoryData<string> BotNames => new(Bots.Names);
    public static TheoryData<string> ProfileNames => new(GeneratorSettings.ProfileNames);

    [Theory]
    [MemberData(nameof(BotNames))]
    public void Every_bot_can_be_picked(string bot)
    {
        Assert.Equal(bot, Parse("--bot", bot).Bot);
    }

    [Theory]
    [MemberData(nameof(ProfileNames))]
    public void Every_profile_can_be_picked(string profile)
    {
        Assert.Equal(new[] { profile }, Parse("--profile", profile).Profiles);
    }

    [Fact]
    public void All_profiles_run_by_default_and_with_all()
    {
        Assert.Equal(GeneratorSettings.ProfileNames, Parse().Profiles);
        Assert.Equal(GeneratorSettings.ProfileNames, Parse("--profile", "all").Profiles);
    }

    [Theory]
    [InlineData("--bot", "nope")]
    [InlineData("--profile", "nope")]
    public void Unknown_bots_and_profiles_are_rejected(string option, string value)
    {
        Assert.Throws<ArgumentException>(() => SimOptions.Parse(new[] { option, value }));
    }

    [Fact]
    public void Usage_names_every_bot_and_profile()
    {
        Assert.All(Bots.Names, bot => Assert.Contains(bot, SimOptions.Usage));
        Assert.All(GeneratorSettings.ProfileNames, profile => Assert.Contains(profile, SimOptions.Usage));
    }

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
