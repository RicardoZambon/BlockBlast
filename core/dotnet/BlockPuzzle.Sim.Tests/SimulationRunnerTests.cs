namespace BlockPuzzle.Sim.Tests;

public class SimulationRunnerTests
{
    [Theory]
    [InlineData(7, 3)]
    [InlineData(6, 3)]
    [InlineData(10, 50)]
    public void Runs_exactly_the_requested_number_of_games(int games, int gamesPerPlayer)
    {
        var results = SimulationRunner.Run(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), "random", games, gamesPerPlayer, seed: 1);

        Assert.Equal(games, results.Length);
        Assert.All(results, r => Assert.True(r.Placements > 0));
    }

    [Fact]
    public void A_seeded_run_always_plays_out_the_same()
    {
        // Golden value: pins each game's seed and slot in the results, the random bot's seed, and the
        // quick losses that make a player's next game a mercy game (m). If it moves, the simulator's numbers move too.
        var results = SimulationRunner.Run(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), "random", games: 7, gamesPerPlayer: 3, seed: 1);

        Assert.Equal("17/122 20/136 26/271 13/55 10/50 10/37m 14/70", string.Join(" ", results.Select(r => $"{r.Placements}/{r.Score}{(r.Mercy ? "m" : "")}")));
    }

    [Fact]
    public void An_unknown_bot_is_rejected_before_any_game_starts()
    {
        Assert.Throws<ArgumentException>(() =>
            SimulationRunner.Run(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), "nope", games: 10, gamesPerPlayer: 5, seed: 1));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public void Counts_below_1_are_rejected(int games, int gamesPerPlayer)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SimulationRunner.Run(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), "random", games, gamesPerPlayer, seed: 1));
    }
}
