#nullable enable
using System;
using BlockPuzzle.Core;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class RetentionRulesTests
    {
        const int Day = 20747;   // 2026-10-21

        [Test]
        public void A_rating_is_asked_for_after_a_new_best_once_enough_games_are_played()
        {
            Assert.IsTrue(RetentionRules.ShouldAskForRating(newBest: true, gamesFinished: 5, lastAskedDay: -1, today: Day));
            Assert.IsFalse(RetentionRules.ShouldAskForRating(newBest: false, gamesFinished: 50, lastAskedDay: -1, today: Day), "only on a new best");
            Assert.IsFalse(RetentionRules.ShouldAskForRating(newBest: true, gamesFinished: 4, lastAskedDay: -1, today: Day), "not before 5 games");
        }

        [Test]
        public void A_rating_is_asked_for_at_most_every_60_days()
        {
            Assert.IsFalse(RetentionRules.ShouldAskForRating(true, 20, lastAskedDay: Day - 59, today: Day));
            Assert.IsTrue(RetentionRules.ShouldAskForRating(true, 20, lastAskedDay: Day - 60, today: Day));
            Assert.IsFalse(RetentionRules.ShouldAskForRating(true, 20, lastAskedDay: Day + 3, today: Day), "a clock set back doesn't ask again");
        }

        [Test]
        public void Reminders_are_asked_for_once_after_the_first_daily()
        {
            Assert.IsFalse(RetentionRules.ShouldAskForReminders(new DailyRecord(), askedBefore: false), "not before any Daily");
            var played = new DailyRecord();
            played.Finish(Day, 400);
            Assert.IsTrue(RetentionRules.ShouldAskForReminders(played, askedBefore: false));
            Assert.IsFalse(RetentionRules.ShouldAskForReminders(played, askedBefore: true));
        }

        // Day D's puzzle stays until midnight UTC; the reminder comes at the first 18:00 local after that, when a new one is out.
        [TestCase(-3, "2026-10-22 21:00")]   // Brazil: the next puzzle appears at 21:00 local on the 21st, so 18:00 local on the 22nd
        [TestCase(0, "2026-10-22 18:00")]
        [TestCase(7, "2026-10-22 11:00")]    // Jakarta: the next puzzle appears at 07:00 local on the 22nd, so 18:00 local that day
        public void The_reminder_comes_at_18_local_once_an_unplayed_puzzle_is_out(int utcOffsetHours, string expectedUtc)
        {
            DateTime fire = RetentionRules.NextReminderUtc(lastPlayedDay: Day, TimeSpan.FromHours(utcOffsetHours));

            Assert.AreEqual(DateTimeKind.Utc, fire.Kind);
            Assert.AreEqual(expectedUtc, fire.ToString("yyyy-MM-dd HH:mm"));
            Assert.Greater(DailyPuzzle.DayNumber(fire), Day, "never while the played puzzle is still today's");
        }
    }
}
