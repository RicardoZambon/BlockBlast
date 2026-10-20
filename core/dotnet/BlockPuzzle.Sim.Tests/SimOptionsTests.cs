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
}
