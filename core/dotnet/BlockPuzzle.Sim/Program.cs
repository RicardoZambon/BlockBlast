using System.Diagnostics;
using BlockPuzzle.Core;
using BlockPuzzle.Sim;

SimOptions? options;
try
{
    options = SimOptions.Parse(args);
}
catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine(SimOptions.Usage);
    return 1;
}
if (options is null)
{
    Console.WriteLine(SimOptions.Usage);
    return 0;
}

Console.WriteLine($"{options.Games:N0} games per profile " +
                  $"({options.Players:N0} players, up to {options.GamesPerPlayer} games each), bot: {options.Bot}, seed: {options.Seed}");
Console.WriteLine();
Console.WriteLine(Report.Header);

GameResult[]? baselineResults = null;
foreach (string profile in options.Profiles)
{
    var settings = GeneratorSettings.ForProfile(profile);
    var stopwatch = Stopwatch.StartNew();
    var results = SimulationRunner.Run(PieceCatalog.Standard, settings, new ScoringSettings(), options.Bot, options.Games, options.GamesPerPlayer, options.Seed);
    Console.WriteLine(Report.Row(profile, options.Bot, results, stopwatch.Elapsed));
    if (profile == "baseline") baselineResults = results;
}

Console.WriteLine();
Console.WriteLine("Columns are placements per game except: <20 = games ending before placement 20, lines = lines per game,");
Console.WriteLine("score = median score, streak90 = 90th-percentile best streak, mercy = mercy games, capped = games stopped at the cap.");
Console.WriteLine($"Target for baseline + greedy: {TargetCheck.Description}.");

if (options.CheckTarget)
{
    // Parse guarantees the greedy bot and the baseline profile, so baselineResults is set.
    string? miss = TargetCheck.Miss(baselineResults!);
    if (miss != null)
    {
        Console.Error.WriteLine($"Target missed: {miss}.");
        return 2;
    }
    Console.WriteLine("Target met.");
}
return 0;
