namespace BlockPuzzle.Sim;

/// <summary>The Phase 1 difficulty target for the baseline profile played by the greedy bot. CI enforces it.</summary>
public static class TargetCheck
{
    public const int MinMedianPlacements = 80;
    public const int MaxMedianPlacements = 150;
    public const double MaxPercentEndingEarly = 5.0;

    public static string Description =>
        $"median {MinMedianPlacements}-{MaxMedianPlacements} placements, under {MaxPercentEndingEarly}% of games ending before placement {Report.EarlyEndPlacements}";

    /// <summary>Null when the results meet the target; otherwise what missed it.</summary>
    public static string? Miss(GameResult[] results)
    {
        var misses = new List<string>();

        int median = Report.MedianPlacements(results);
        if (median < MinMedianPlacements || median > MaxMedianPlacements)
            misses.Add($"median {median} placements is outside {MinMedianPlacements}-{MaxMedianPlacements}");

        double early = Report.PercentEndingEarly(results);
        if (early >= MaxPercentEndingEarly)
            misses.Add($"{early:F1}% of games end before placement {Report.EarlyEndPlacements} (limit: under {MaxPercentEndingEarly}%)");

        return misses.Count == 0 ? null : string.Join("; ", misses);
    }
}
