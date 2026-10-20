#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// A player's Daily history: the last day they finished, its score, and the streak of days in a row. Days are
    /// DailyPuzzle.DayNumber values. Public fields, so Unity's JsonUtility can save it.
    /// </summary>
    [Serializable]
    public sealed class DailyRecord
    {
        /// <summary>The last day with a finished attempt; −1 before the first.</summary>
        public int LastDay = -1;
        public int LastScore;
        public int Streak;
        public int BestStreak;
        public int BestScore;

        /// <summary>Whether a scored attempt can start: not on a day already scored, and not after the clock went back before it.</summary>
        public bool CanStart(int today) => today > LastDay;

        /// <summary>The streak as it stands today: it lapses once a whole day passes without a Daily.</summary>
        public int StreakOn(int today) => today <= LastDay + 1 ? Streak : 0;

        /// <summary>Records an attempt finished for a day. Ignored unless the day comes after LastDay, so a day scores once.</summary>
        public void Finish(int day, int score)
        {
            if (day <= LastDay) return;
            Streak = day == LastDay + 1 ? Streak + 1 : 1;
            LastDay = day;
            LastScore = score;
            if (Streak > BestStreak) BestStreak = Streak;
            if (score > BestScore) BestScore = score;
        }
    }
}
