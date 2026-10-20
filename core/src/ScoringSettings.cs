#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Scoring values. Public fields so Unity's JsonUtility and Remote Config can fill them.</summary>
    [Serializable]
    public sealed class ScoringSettings
    {
        /// <summary>Upper limit for every value, so a typo can't produce absurd scores.</summary>
        public const int MaxValue = 1_000_000;

        public int PointsPerCell = 1;
        /// <summary>Clearing n lines at once scores LineBase * n(n+1)/2: 10, 30, 60, 100...</summary>
        public int LineBase = 10;
        /// <summary>Each streak level after the first adds this percentage to line points.</summary>
        public int StreakBonusPercent = 50;
        /// <summary>The streak resets after this many placements in a row without a clear.</summary>
        public int StreakResetAfterMisses = 3;
        public int BoardClearBonus = 300;

        /// <summary>Throws if a value could break scoring, for example a negative Remote Config value.</summary>
        public void Validate()
        {
            if (!IsValid(PointsPerCell) || !IsValid(LineBase) || !IsValid(StreakBonusPercent) || !IsValid(BoardClearBonus))
                throw new ArgumentException($"Scoring values must be between 0 and {MaxValue}.");
            if (StreakResetAfterMisses < 1 || StreakResetAfterMisses > MaxValue)
                throw new ArgumentException($"StreakResetAfterMisses must be between 1 and {MaxValue}.");
        }

        /// <summary>A copy, so edits to the original can't change a game in progress.</summary>
        public ScoringSettings Clone() => (ScoringSettings)MemberwiseClone();

        static bool IsValid(int value) => value >= 0 && value <= MaxValue;
    }
}
