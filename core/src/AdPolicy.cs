#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Decides when an interstitial may show. Pure logic, so it can be unit-tested.</summary>
    public static class AdPolicy
    {
        /// <param name="secondsSinceLastFullScreenAd">Null if no full-screen ad has been shown yet.</param>
        /// <param name="betweenGames">True only after a game ended, when the player taps Play Again or Home.</param>
        public static bool CanShowInterstitial(AdPolicySettings settings, bool ownsRemoveAds, int gamesCompleted, double? secondsSinceLastFullScreenAd, bool betweenGames)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!settings.InterstitialsEnabled || ownsRemoveAds || !betweenGames) return false;
            if (gamesCompleted < settings.GraceGames) return false;
            return secondsSinceLastFullScreenAd == null || secondsSinceLastFullScreenAd.Value >= settings.CooldownSeconds;
        }
    }
}
