namespace BlockPuzzle.Core.Tests;

public class DailyPuzzleTests
{
    const int October20 = 20746;   // 2026-10-20: 56 years of 365 days, 14 leap days, then 292 days into 2026

    [Fact]
    public void Day_numbers_count_whole_utc_days_from_1970()
    {
        Assert.Equal(0, DailyPuzzle.DayNumber(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(1, DailyPuzzle.DayNumber(new DateTime(1970, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(October20, DailyPuzzle.DayNumber(new DateTime(2026, 10, 20, 23, 59, 59, DateTimeKind.Utc)));
        Assert.Equal(October20 + 1, DailyPuzzle.DayNumber(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc)));

        // Just after and just before midnight UTC, so in any zone but UTC one of the two local times is on another date.
        foreach (int hour in new[] { 0, 23 })
        {
            var utc = new DateTime(2026, 10, 20, hour, 30, 0, DateTimeKind.Utc);
            Assert.Equal(October20, DailyPuzzle.DayNumber(utc.ToLocalTime()));
            Assert.Equal(October20, DailyPuzzle.DayNumber(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)));
        }
    }

    [Fact]
    public void Days_before_1970_round_down()
    {
        Assert.Equal(-1, DailyPuzzle.DayNumber(new DateTime(1969, 12, 31, 23, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(-1, DailyPuzzle.DayNumber(new DateTime(1969, 12, 31, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(-2, DailyPuzzle.DayNumber(new DateTime(1969, 12, 30, 23, 59, 59, DateTimeKind.Utc)));
    }

    [Fact]
    public void The_date_of_a_day_is_its_utc_midnight()
    {
        var date = DailyPuzzle.DateOf(October20);

        Assert.Equal(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), date);
        Assert.Equal(DateTimeKind.Utc, date.Kind);
    }

    // Golden values: every phone must deal the same Daily, in every app version.
    [Fact]
    public void Each_day_has_its_own_seed_and_it_never_changes()
    {
        Assert.Equal(4023608590332759023UL, DailyPuzzle.Seed(October20));
        Assert.NotEqual(DailyPuzzle.Seed(October20), DailyPuzzle.Seed(October20 + 1));
    }

    [Fact]
    public void A_daily_game_deals_the_same_first_tray_everywhere()
    {
        var game = DailyPuzzle.NewGame(October20);

        Assert.Equal("c3d,t4a,c3a", string.Join(",", Enumerable.Range(0, 3).Select(slot => game.GetTrayPiece(slot)!.Id)));
        Assert.Equal(0, game.GameIndex);
        Assert.False(game.IsMercyGame);
    }

    [Fact]
    public void The_first_daily_starts_a_one_day_streak()
    {
        var record = new DailyRecord();
        Assert.True(record.CanStart(100));

        record.Finish(100, 500);

        Assert.Equal((100, 500, 1, 1, 500), (record.LastDay, record.LastScore, record.Streak, record.BestStreak, record.BestScore));
    }

    [Fact]
    public void The_next_day_extends_the_streak()
    {
        var record = new DailyRecord();
        record.Finish(100, 500);

        record.Finish(101, 300);

        Assert.Equal(2, record.Streak);
        Assert.Equal(2, record.BestStreak);
        Assert.Equal(300, record.LastScore);
    }

    [Fact]
    public void A_missed_day_restarts_the_streak()
    {
        var record = new DailyRecord();
        record.Finish(100, 500);
        record.Finish(101, 500);

        record.Finish(103, 500);

        Assert.Equal(1, record.Streak);
        Assert.Equal(2, record.BestStreak);
    }

    [Fact]
    public void A_day_scores_only_once()
    {
        var record = new DailyRecord();
        record.Finish(100, 500);

        Assert.False(record.CanStart(100));
        record.Finish(100, 900);

        Assert.Equal(500, record.LastScore);
        Assert.Equal(500, record.BestScore);
        Assert.Equal(1, record.Streak);
    }

    [Fact]
    public void Setting_the_clock_back_cannot_start_or_score_an_earlier_day()
    {
        var record = new DailyRecord();
        record.Finish(100, 500);

        Assert.False(record.CanStart(99));
        record.Finish(99, 900);

        Assert.Equal(100, record.LastDay);
        Assert.Equal(500, record.BestScore);
    }

    [Fact]
    public void The_streak_lapses_after_a_missed_day()
    {
        var record = new DailyRecord();
        Assert.Equal(0, record.StreakOn(100));
        record.Finish(100, 500);
        record.Finish(101, 500);

        Assert.Equal(2, record.StreakOn(101));
        Assert.Equal(2, record.StreakOn(102));
        Assert.Equal(0, record.StreakOn(103));
        Assert.Equal(2, record.StreakOn(99));   // a clock set back doesn't hide the streak
    }

    [Fact]
    public void The_bests_keep_the_highest()
    {
        var record = new DailyRecord();
        record.Finish(100, 900);
        record.Finish(101, 300);

        record.Finish(105, 400);

        Assert.Equal(900, record.BestScore);
        Assert.Equal(2, record.BestStreak);
    }
}
