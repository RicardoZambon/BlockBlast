namespace BlockPuzzle.Core.Tests;

public class ScoringTests
{
    static readonly ScoringSettings Defaults = new();

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 30)]
    [InlineData(3, 60)]
    [InlineData(4, 100)]
    public void Line_points_grow_with_lines_cleared_at_once(int lines, int expected)
    {
        Assert.Equal(expected, Scoring.LinePoints(Defaults, lines));
    }

    [Fact]
    public void A_placement_without_clears_scores_its_cells()
    {
        var state = Scoring.Apply(Defaults, default, cellsPlaced: 4, lines: 0, boardCleared: false, out int points);

        Assert.Equal(4, points);
        Assert.Equal(4, state.Score);
        Assert.Equal(0, state.Streak);
    }

    [Fact]
    public void Consecutive_clears_build_a_streak_multiplier()
    {
        var first = Scoring.Apply(Defaults, default, 3, 1, false, out int p1);   // streak 1: 3 + 10
        var second = Scoring.Apply(Defaults, first, 3, 1, false, out int p2);    // streak 2: 3 + 10 * 1.5
        var third = Scoring.Apply(Defaults, second, 3, 2, false, out int p3);    // streak 3: 3 + 30 * 2.0

        Assert.Equal(13, p1);
        Assert.Equal(18, p2);
        Assert.Equal(63, p3);
        Assert.Equal(3, third.Streak);
        Assert.Equal(3, third.MaxStreak);
        Assert.Equal(13 + 18 + 63, third.Score);
    }

    [Fact]
    public void The_streak_survives_two_misses_and_resets_on_the_third()
    {
        var state = Scoring.Apply(Defaults, default, 1, 1, false, out _);
        state = Scoring.Apply(Defaults, state, 1, 0, false, out _);
        state = Scoring.Apply(Defaults, state, 1, 0, false, out _);

        Assert.Equal(1, state.Streak);
        Assert.Equal(2, Scoring.Apply(Defaults, state, 1, 1, false, out _).Streak);

        state = Scoring.Apply(Defaults, state, 1, 0, false, out _);

        Assert.Equal(0, state.Streak);
        Assert.Equal(1, Scoring.Apply(Defaults, state, 1, 1, false, out _).Streak);
    }

    [Fact]
    public void Validate_rejects_values_that_could_make_scores_negative()
    {
        Assert.Throws<ArgumentException>(() => new ScoringSettings { PointsPerCell = -1 }.Validate());
        Assert.Throws<ArgumentException>(() => new ScoringSettings { LineBase = -10 }.Validate());
        Assert.Throws<ArgumentException>(() => new ScoringSettings { StreakBonusPercent = -150 }.Validate());
        Assert.Throws<ArgumentException>(() => new ScoringSettings { BoardClearBonus = -1 }.Validate());
        Assert.Throws<ArgumentException>(() => new ScoringSettings { StreakResetAfterMisses = 0 }.Validate());
        Assert.Throws<ArgumentException>(() => new ScoringSettings { LineBase = 2_000_000 }.Validate());
        new ScoringSettings().Validate();
    }

    [Fact]
    public void The_score_stops_at_the_maximum_instead_of_wrapping()
    {
        var extreme = new ScoringSettings { LineBase = ScoringSettings.MaxValue, StreakBonusPercent = ScoringSettings.MaxValue };

        var state = Scoring.Apply(extreme, new ScoreState(int.MaxValue - 5, 1000, 0, 1000), cellsPlaced: 9, lines: 6, boardCleared: false, out int points);

        Assert.Equal(int.MaxValue, state.Score);
        Assert.Equal(5, points);
    }

    [Fact]
    public void The_best_streak_is_kept_after_the_streak_resets()
    {
        var state = default(ScoreState);
        for (int i = 0; i < 3; i++) state = Scoring.Apply(Defaults, state, 1, 1, false, out _);   // streak 3
        for (int i = 0; i < 3; i++) state = Scoring.Apply(Defaults, state, 1, 0, false, out _);   // three misses reset it

        Assert.Equal(0, state.Streak);
        Assert.Equal(3, state.MaxStreak);
    }

    [Fact]
    public void A_board_clear_during_a_streak_gets_the_flat_bonus()
    {
        var onStreak = new ScoreState(score: 0, streak: 2, missesSinceClear: 0, maxStreak: 2);

        Scoring.Apply(Defaults, onStreak, cellsPlaced: 1, lines: 1, boardCleared: true, out int points);

        // Streak 3 doubles the line points (10 becomes 20); the +300 board-clear bonus isn't multiplied.
        Assert.Equal(1 + 20 + 300, points);
    }

    [Fact]
    public void Clearing_the_whole_board_adds_the_bonus()
    {
        Scoring.Apply(Defaults, default, 1, 1, boardCleared: true, out int points);

        Assert.Equal(1 + 10 + 300, points);
    }
}
