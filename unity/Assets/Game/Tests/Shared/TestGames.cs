#nullable enable
using System.Linq;
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

        public static string TrayIds(GameSession game) => string.Join(",", game.Tray.ToArray().Select(p => p?.Id ?? "-"));

        /// <summary>Saves a game in progress in the given position, so the next boot resumes it instead of dealing at random.</summary>
        public static void Save(string savePath, GameSession position, int gameIndex = 0, int recentQuickLosses = 0, int nextGameIndex = 0)
        {
            var snapshot = position.ToSnapshot();
            snapshot.GameIndex = gameIndex;
            snapshot.RecentQuickLosses = recentQuickLosses;
            new SaveStore(savePath).Write(new SaveData { HasGame = true, Game = snapshot, NextGameIndex = nextGameIndex });
        }
    }
}
