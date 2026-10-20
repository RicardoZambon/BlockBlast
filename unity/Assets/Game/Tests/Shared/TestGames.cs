#nullable enable
using BlockPuzzle.Core;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>Games in chosen positions, built through a snapshot the way the engine's own tests do.</summary>
    public static class TestGames
    {
        public static GameSession Position(Board board, params string[] tray)
        {
            var rng = new Pcg32(99);
            var snapshot = new GameSnapshot
            {
                Board = board.Cells,
                Tray = tray,
                RngState = rng.State,
                RngIncrement = rng.Increment,
            };
            return GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings());
        }
    }
}
