#nullable enable
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Engine checks that run anywhere the game runs: the first-fit golden game from the engine's own tests,
    /// so a device can prove it deals exactly like .NET does.
    /// </summary>
    public static class EngineChecks
    {
        public const ulong GoldenSeed = 20261020UL;

        /// <summary>The end of the golden first-fit game; core/dotnet/BlockPuzzle.Core.Tests/DeterminismTests.cs pins the same value.</summary>
        public const string FirstFitGolden = "placements=43 score=459 lines=15 board=22663EFEC7D03F24";

        /// <summary>Plays the first legal move, scanning tray slots in order and positions top-left first. False if nothing fits.</summary>
        public static bool PlayFirstFitMove(GameSession game)
        {
            for (int slot = 0; slot < PieceGenerator.TraySize; slot++)
            {
                var shape = game.GetTrayPiece(slot);
                if (shape == null) continue;
                for (int i = 0; i < shape.PlacementCount; i++)
                {
                    if (game.TryPlace(slot, shape.AnchorRow(i), shape.AnchorCol(i), out _)) return true;
                }
            }
            return false;
        }

        /// <summary>Plays a seeded Baseline game first-fit until it ends (at most 10,000 moves) and describes the end state.</summary>
        public static string PlayFirstFit(ulong seed)
        {
            var game = new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), seed);
            for (int i = 0; i < 10_000 && !game.IsGameOver; i++) PlayFirstFitMove(game);
            return $"placements={game.Placements} score={game.Score} lines={game.LinesCleared} board={game.Board.Cells:X16}";
        }
    }
}
