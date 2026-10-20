namespace BlockPuzzle.Sim.Tests;

public class GreedyBotTests
{
    [Fact]
    public void A_seeded_game_always_plays_out_the_same()
    {
        // Golden value: if a change to GreedyBot moves it, the simulator results move too.
        var game = new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), seed: 20261020);
        var bot = new GreedyBot();

        while (!game.IsGameOver && bot.PlayOneMove(game)) { }

        Assert.Equal("placements=98 score=1589 lines=41 board=393B1B99F8000FB3", $"placements={game.Placements} score={game.Score} lines={game.LinesCleared} board={game.Board.Cells:X16}");
    }
}
