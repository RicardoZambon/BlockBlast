#nullable enable
using System;
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>When to ask for a rating, and when to ask about and send the Daily reminder (master plan 3.7). Pure.</summary>
    public static class RetentionRules
    {
        /// <summary>review_prompt_min_games, until Remote Config can set it (3.1).</summary>
        public const int ReviewPromptMinGames = 5;

        /// <summary>The app's own gap between rating requests; the stores add stricter limits of their own.</summary>
        public const int ReviewPromptGapDays = 60;

        /// <summary>The reminder's local hour.</summary>
        public const int ReminderHour = 18;

        /// <param name="lastAskedDay">The day the app last asked, as a DailyPuzzle.DayNumber; −1 if never.</param>
        public static bool ShouldAskForRating(bool newBest, int gamesFinished, int lastAskedDay, int today) =>
            newBest && gamesFinished >= ReviewPromptMinGames &&
            (lastAskedDay < 0 || today - lastAskedDay >= ReviewPromptGapDays);

        /// <summary>Only after the player's first finished Daily, and only once.</summary>
        public static bool ShouldAskForReminders(DailyRecord record, bool askedBefore) => record.LastDay >= 0 && !askedBefore;

        /// <summary>
        /// The first 18:00 local after the next puzzle comes out, at midnight UTC after the day just played, so the
        /// reminder never points at a puzzle the player already finished. In UTC.
        /// </summary>
        public static DateTime NextReminderUtc(int lastPlayedDay, TimeSpan utcOffset)
        {
            DateTime nextPuzzleLocal = DailyPuzzle.DateOf(lastPlayedDay + 1) + utcOffset;
            DateTime reminderLocal = nextPuzzleLocal.Date.AddHours(ReminderHour);
            if (reminderLocal < nextPuzzleLocal) reminderLocal = reminderLocal.AddDays(1);
            return DateTime.SpecifyKind(reminderLocal - utcOffset, DateTimeKind.Utc);
        }
    }
}
