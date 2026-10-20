#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// Everything needed to resume a game exactly, including the random generator's position.
    /// Public fields so both Unity's JsonUtility and System.Text.Json (with IncludeFields) can save it.
    /// </summary>
    [Serializable]
    public sealed class GameSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public ulong Board;
        /// <summary>Shape id per tray slot; an empty string means the slot was already used.</summary>
        public string[] Tray = Array.Empty<string>();
        public int Score;
        public int Streak;
        public int MissesSinceClear;
        public int MaxStreak;
        public int Placements;
        public int LinesCleared;
        public ulong RngState;
        public ulong RngIncrement;
        public int GameIndex;
        public int RecentQuickLosses;
        public bool Revived;
    }
}
