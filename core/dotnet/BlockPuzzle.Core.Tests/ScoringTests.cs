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
    public void Clearing_the_whole_board_adds_the_bonus()
    {
        Scoring.Apply(Defaults, default, 1, 1, boardCleared: true, out int points);

        Assert.Equal(1 + 10 + 300, points);
    }
}
