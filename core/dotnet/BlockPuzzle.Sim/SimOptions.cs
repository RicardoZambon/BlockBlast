using BlockPuzzle.Core;

namespace BlockPuzzle.Sim;

/// <summary>Command-line options for the simulator.</summary>
public sealed class SimOptions
{
    public int Games { get; private set; } = 20_000;
    public int GamesPerPlayer { get; private set; } = 50;
    public string Bot { get; private set; } = "greedy";
    public IReadOnlyList<string> Profiles { get; private set; } = GeneratorSettings.ProfileNames;
    public ulong Seed { get; private set; } = 1;
    public bool CheckTarget { get; private set; }

    /// <summary>
    /// Simulated players needed to play exactly Games games. Each plays up to GamesPerPlayer games in a row,
    /// so quick losses can trigger mercy; the last player may play fewer.
    /// </summary>
    public int Players => (int)(((long)Games + GamesPerPlayer - 1) / GamesPerPlayer);

    public static string Usage { get; } = $"""
        Usage: dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- [options]

          --games N             total games per profile (default 20000)
          --games-per-player N  games each simulated player plays in a row (default 50; the last may play fewer)
          --bot NAME            who plays: {string.Join(" or ", Bots.Names)} (default greedy)
          --profile NAME        {string.Join(", ", GeneratorSettings.ProfileNames)} or all (default all)
          --seed N              base seed (default 1)
          --check-target        exit with code 2 if {TargetCheck.Profile} + {TargetCheck.Bot} misses the difficulty target
        """;

    /// <summary>Returns null for --help; throws ArgumentException or FormatException for bad input.</summary>
    public static SimOptions? Parse(string[] args)
    {
        var options = new SimOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

            switch (args[i])
            {
                case "--games": options.Games = int.Parse(Value()); break;
                case "--games-per-player": options.GamesPerPlayer = int.Parse(Value()); break;
                case "--bot": options.Bot = Value(); break;
                case "--profile":
                    string profile = Value();
                    options.Profiles = profile == "all" ? GeneratorSettings.ProfileNames : new[] { profile };
                    break;
                case "--seed": options.Seed = ulong.Parse(Value()); break;
                case "--check-target": options.CheckTarget = true; break;
                case "--help" or "-h": return null;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        if (!Bots.Names.Contains(options.Bot)) throw new ArgumentException($"--bot must be {string.Join(" or ", Bots.Names)}.");
        if (options.Games < 1 || options.GamesPerPlayer < 1) throw new ArgumentException("--games and --games-per-player must be positive.");
        foreach (string profile in options.Profiles)
        {
            if (!GeneratorSettings.ProfileNames.Contains(profile)) throw new ArgumentException($"Unknown profile '{profile}'.");
        }
        if (options.CheckTarget && (options.Bot != TargetCheck.Bot || !options.Profiles.Contains(TargetCheck.Profile)))
            throw new ArgumentException($"--check-target needs the {TargetCheck.Bot} bot and the {TargetCheck.Profile} profile.");
        return options;
    }
}
