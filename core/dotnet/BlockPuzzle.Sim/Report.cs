namespace BlockPuzzle.Sim;

/// <summary>Formats one table row per profile.</summary>
public static class Report
{
    /// <summary>A game that ends before this placement counts as ending early.</summary>
    public const int EarlyEndPlacements = 20;

    public static string Header =>
        $"{"profile",-10}{"bot",-8}{"games",8}{"median",8}{"p10",6}{"p90",7}{"mean",8}{"<" + EarlyEndPlacements,8}{"lines",8}{"score",9}{"streak90",10}{"mercy",8}{"capped",8}{"secs",7}";

    public static string Row(string profile, string bot, GameResult[] results, TimeSpan elapsed)
    {
        int[] placements = results.Select(r => r.Placements).Order().ToArray();
        int[] scores = results.Select(r => r.Score).Order().ToArray();
        int[] streaks = results.Select(r => r.MaxStreak).Order().ToArray();

        return $"{profile,-10}{bot,-8}{results.Length,8}" +
               $"{Percentile(placements, 50),8}{Percentile(placements, 10),6}{Percentile(placements, 90),7}{placements.Average(),8:F1}" +
               $"{PercentEndingEarly(results),7:F1}%{results.Average(r => r.Lines),8:F1}" +
               $"{Percentile(scores, 50),9}{Percentile(streaks, 90),10}" +
               $"{Percent(results, r => r.Mercy),7:F1}%{Percent(results, r => r.Capped),7:F1}%{elapsed.TotalSeconds,7:F1}";
    }

    public static int MedianPlacements(GameResult[] results) =>
        Percentile(results.Select(r => r.Placements).Order().ToArray(), 50);

    public static double PercentEndingEarly(GameResult[] results) =>
        Percent(results, r => r.Placements < EarlyEndPlacements);

    static int Percentile(int[] sorted, int percent) => sorted[(int)Math.Round((sorted.Length - 1) * percent / 100.0)];

    static double Percent(GameResult[] results, Func<GameResult, bool> predicate) =>
        100.0 * results.Count(predicate) / results.Length;
}
