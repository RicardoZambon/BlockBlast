using BlockPuzzle.Core;

namespace BlockPuzzle.Sim;

public readonly record struct GameResult(int Placements, int Score, int Lines, int MaxStreak, bool Mercy, bool Capped);

/// <summary>Plays many games in parallel. Deterministic: every player and game has its own derived seed.</summary>
public static class SimulationRunner
{
    /// <summary>Stops endless games; a high share of capped games means the profile is far too easy.</summary>
    public const int PlacementCap = 10_000;

    public static GameResult[] Run(PieceCatalog catalog, GeneratorSettings generator, ScoringSettings scoring, string bot, int players, int gamesPerPlayer, ulong seed)
    {
        var results = new GameResult[players * gamesPerPlayer];
        Parallel.For(0, players, player =>
        {
            IBot playerBot = bot == "random" ? new RandomBot(Mix(seed, (ulong)player, 0xB07)) : new GreedyBot();
            int quickLosses = 0;
            for (int g = 0; g < gamesPerPlayer; g++)
            {
                var game = new GameSession(catalog, generator, scoring, Mix(seed, (ulong)player, (ulong)g), g, quickLosses);
                bool capped = false;
                while (!game.IsGameOver)
                {
                    if (game.Placements >= PlacementCap)
                    {
                        capped = true;
                        break;
                    }
                    if (!playerBot.PlayOneMove(game)) break;
                }

                results[player * gamesPerPlayer + g] = new GameResult(game.Placements, game.Score, game.LinesCleared, game.MaxStreak, game.IsMercyGame, capped);
                quickLosses = game.Placements < generator.QuickLossPlacements ? quickLosses + 1 : 0;
            }
        });
        return results;
    }

    /// <summary>SplitMix64 finalizer over the inputs: well-spread, reproducible seeds.</summary>
    static ulong Mix(ulong a, ulong b, ulong c)
    {
        ulong z = a ^ (b * 0x9E3779B97F4A7C15UL) ^ (c * 0xC2B2AE3D27D4EB4FUL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
