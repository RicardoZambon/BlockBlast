#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Running score values for one game.</summary>
    public readonly struct ScoreState
    {
        public readonly int Score;
        public readonly int Streak;
        public readonly int MissesSinceClear;
        public readonly int MaxStreak;

        public ScoreState(int score, int streak, int missesSinceClear, int maxStreak)
        {
            Score = score;
            Streak = streak;
            MissesSinceClear = missesSinceClear;
            MaxStreak = maxStreak;
        }
    }

    public static class Scoring
    {
        public static int LinePoints(ScoringSettings settings, int lines) =>
            lines <= 0 ? 0 : settings.LineBase * lines * (lines + 1) / 2;

        /// <summary>Scores one placement and returns the new running state.</summary>
        public static ScoreState Apply(ScoringSettings settings, ScoreState state, int cellsPlaced, int lines, bool boardCleared, out int points)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            int streak = state.Streak;
            int misses = state.MissesSinceClear;
            long linePoints = 0;
            if (lines > 0)
            {
                streak++;
                misses = 0;
                long multiplierPercent = 100L + (long)settings.StreakBonusPercent * (streak - 1);
                linePoints = LinePoints(settings, lines) * multiplierPercent / 100;
            }
            else
            {
                misses++;
                if (misses >= settings.StreakResetAfterMisses) streak = 0;
            }

            // Long arithmetic, capped so the running score stops at int.MaxValue instead of wrapping negative.
            long total = (long)cellsPlaced * settings.PointsPerCell + linePoints + (boardCleared ? settings.BoardClearBonus : 0);
            points = (int)Math.Min(total, (long)int.MaxValue - state.Score);
            return new ScoreState(state.Score + points, streak, misses, Math.Max(state.MaxStreak, streak));
        }
    }
}
