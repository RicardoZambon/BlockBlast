namespace BlockPuzzle.Sim;

/// <summary>Command-line options for the simulator.</summary>
public sealed class SimOptions
{
    static readonly string[] AllProfiles = { "gentle", "baseline", "hard" };

    public int Games { get; private set; } = 20_000;
    public int GamesPerPlayer { get; private set; } = 50;
    public string Bot { get; private set; } = "greedy";
    public IReadOnlyList<string> Profiles { get; private set; } = AllProfiles;
    public ulong Seed { get; private set; } = 1;

    /// <summary>Each simulated player plays GamesPerPlayer games in a row, so quick losses can trigger mercy.</summary>
    public int Players => Math.Max(1, Games / GamesPerPlayer);

    public const string Usage = """
        Usage: dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- [options]

          --games N             total games per profile (default 20000)
          --games-per-player N  games each simulated player plays in a row (default 50)
          --bot greedy|random   who plays (default greedy)
          --profile NAME        gentle, baseline, hard or all (default all)
          --seed N              base seed (default 1)
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
                    options.Profiles = profile == "all" ? AllProfiles : new[] { profile };
                    break;
                case "--seed": options.Seed = ulong.Parse(Value()); break;
                case "--help" or "-h": return null;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        if (options.Bot is not ("greedy" or "random")) throw new ArgumentException("--bot must be greedy or random.");
        if (options.Games < 1 || options.GamesPerPlayer < 1) throw new ArgumentException("--games and --games-per-player must be positive.");
        foreach (string profile in options.Profiles)
        {
            if (!AllProfiles.Contains(profile)) throw new ArgumentException($"Unknown profile '{profile}'.");
        }
        return options;
    }
}
