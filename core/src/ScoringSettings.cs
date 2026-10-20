#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Scoring values. Public fields so Unity's JsonUtility and Remote Config can fill them.</summary>
    [Serializable]
    public sealed class ScoringSettings
    {
        public int PointsPerCell = 1;
        /// <summary>Clearing n lines at once scores LineBase * n(n+1)/2: 10, 30, 60, 100...</summary>
        public int LineBase = 10;
        /// <summary>Each streak level after the first adds this percentage to line points.</summary>
        public int StreakBonusPercent = 50;
        /// <summary>The streak resets after this many placements in a row without a clear.</summary>
        public int StreakResetAfterMisses = 3;
        public int BoardClearBonus = 300;
    }
}
