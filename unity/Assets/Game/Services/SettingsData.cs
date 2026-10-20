#nullable enable
using System;

namespace BlockPuzzle.Game
{
    /// <summary>The player's settings. Tasks 2.2 and 2.9 make the toggles do something; for now they are only stored.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public bool Sound = true;
        public bool Music = true;
        public bool Haptics = true;
        public bool ReducedMotion;
    }
}
