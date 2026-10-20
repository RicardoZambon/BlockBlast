#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Interstitial rules. Public fields so Remote Config values can be loaded into them.</summary>
    [Serializable]
    public sealed class AdPolicySettings
    {
        /// <summary>Remote kill switch (Remote Config key ads_inter_enabled).</summary>
        public bool InterstitialsEnabled = true;
        /// <summary>No interstitials until a new player has completed this many games (ads_inter_grace_games).</summary>
        public int GraceGames = 3;
        /// <summary>Minimum seconds between full-screen ads (ads_inter_cooldown_s).</summary>
        public int CooldownSeconds = 90;
    }
}
