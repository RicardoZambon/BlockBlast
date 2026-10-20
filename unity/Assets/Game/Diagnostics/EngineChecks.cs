#nullable enable
using System;
using System.Diagnostics;
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

        /// <summary>"determinism OK" when this runtime plays the golden game exactly as .NET does; otherwise what it got instead.</summary>
        public static string CheckDeterminism()
        {
            string actual = PlayFirstFit(GoldenSeed);
            return actual == FirstFitGolden ? "determinism OK" : "determinism FAILED: " + actual;
        }

        // Crowded boards, the engine tests' Crossroads, Checkerboard and AlmostFullRow, where WholeTray re-deals most
        // and TraySolver searches hardest.
        static readonly Board[] BenchmarkBoards =
        {
            Board.Parse("###.###.", "###.###.", "###.###.", "........", "###.###.", "###.###.", "###.###.", "........"),
            Board.Parse("#.#.#.#.", ".#.#.#.#", "#.#.#.#.", ".#.#.#.#", "#.#.#.#.", ".#.#.#.#", "#.#.#.#.", ".#.#.#.#"),
            Board.Parse("######..", "........", "........", "........", "........", "........", "........", "........"),
        };

        /// <summary>Times Gentle-profile (WholeTray) deals in the helpful phase on crowded boards: "deals: max X ms, mean Y ms".</summary>
        public static string BenchmarkDeals(int deals)
        {
            var generator = new PieceGenerator(PieceCatalog.Standard, GeneratorSettings.Gentle(), new Pcg32(GoldenSeed));
            var tray = new PieceShape?[PieceGenerator.TraySize];
            var stopwatch = new Stopwatch();
            double max = 0;
            double total = 0;
            for (int i = 0; i < deals; i++)
            {
                stopwatch.Restart();
                generator.Deal(BenchmarkBoards[i % BenchmarkBoards.Length], placements: i % 30, mercyGame: false, tray);
                stopwatch.Stop();
                double ms = stopwatch.Elapsed.TotalMilliseconds;
                total += ms;
                if (ms > max) max = ms;
            }
            return $"deals: max {max:F2} ms, mean {total / Math.Max(1, deals):F3} ms";
        }
    }
}
