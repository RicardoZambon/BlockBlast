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
}
