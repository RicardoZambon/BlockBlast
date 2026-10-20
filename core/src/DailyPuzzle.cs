#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// The Daily Puzzle's deal: one per UTC day, the same for every player. The seed comes from the day alone, and a
    /// Daily game is never a mercy game, so two phones given the same moves deal the same pieces.
    /// </summary>
    public static class DailyPuzzle
    {
        const ulong Salt = 0x4441_494C_5950_555AUL;   // "DAILYPUZ"
        const ulong Gamma = 0x9E37_79B9_7F4A_7C15UL;
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Whole UTC days since 1970-01-01. A local time is converted to UTC; an unspecified one counts as UTC.</summary>
        public static int DayNumber(DateTime time)
        {
            if (time.Kind == DateTimeKind.Local) time = time.ToUniversalTime();
            long ticks = time.Ticks - Epoch.Ticks;
            long day = ticks >= 0 ? ticks / TimeSpan.TicksPerDay : (ticks + 1) / TimeSpan.TicksPerDay - 1;   // rounds down before 1970
            return (int)day;
        }

        /// <summary>The UTC midnight that starts a day.</summary>
        public static DateTime DateOf(int day) => Epoch.AddDays(day);

        /// <summary>SplitMix64 of the salt plus the day times the golden ratio, so neighbouring days deal unrelated pieces.</summary>
        public static ulong Seed(int day)
        {
            unchecked
            {
                ulong z = Salt + (ulong)(long)day * Gamma;
                z = (z ^ (z >> 30)) * 0xBF58_476D_1CE4_E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D0_49BB_1331_11EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>A day's Daily: the baseline generator, game index 0 and no quick losses, so mercy never changes it.</summary>
        public static GameSession NewGame(int day) =>
            new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), Seed(day));
    }
}
