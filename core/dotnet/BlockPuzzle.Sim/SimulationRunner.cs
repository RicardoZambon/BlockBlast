using BlockPuzzle.Core;

namespace BlockPuzzle.Sim;

public readonly record struct GameResult(int Placements, int Score, int Lines, int MaxStreak, bool Mercy, bool Capped);

/// <summary>Plays many games in parallel. Deterministic: every player and game has its own derived seed.</summary>
public static class SimulationRunner
{
    /// <summary>Stops endless games; a high share of capped games means the profile is far too easy.</summary>
    public const int PlacementCap = 10_000;

    /// <summary>Plays exactly <paramref name="games"/> games, split into players of up to <paramref name="gamesPerPlayer"/> games each.</summary>
    public static GameResult[] Run(PieceCatalog catalog, GeneratorSettings generator, ScoringSettings scoring, string bot, int games, int gamesPerPlayer, ulong seed)
    {
        // Checked here, so bad input fails before the games start, instead of inside Parallel.For or with empty results.
        if (games < 1) throw new ArgumentOutOfRangeException(nameof(games), "At least one game is needed.");
        if (gamesPerPlayer < 1) throw new ArgumentOutOfRangeException(nameof(gamesPerPlayer), "Each player needs at least one game.");
        if (!Bots.Names.Contains(bot)) throw new ArgumentException($"Unknown bot '{bot}'.", nameof(bot));

        var results = new GameResult[games];
        int players = (int)(((long)games + gamesPerPlayer - 1) / gamesPerPlayer);
        Parallel.For(0, players, player =>
        {
            IBot playerBot = Bots.Create(bot, Mix(seed, (ulong)player, 0xB07));
            int quickLosses = 0;
            int gamesForPlayer = Math.Min(gamesPerPlayer, games - player * gamesPerPlayer);
            for (int g = 0; g < gamesForPlayer; g++)
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
                quickLosses = game.NextRecentQuickLosses;
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
