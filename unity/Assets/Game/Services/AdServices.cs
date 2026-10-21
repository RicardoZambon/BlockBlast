#nullable enable
using System;

namespace BlockPuzzle.Game
{
    /// <summary>The consent flow: asked once at launch, and nothing that collects data starts before it finishes (3.2).</summary>
    public interface IConsentFlow
    {
        void Request(Action finished);
    }

    /// <summary>An ad SDK, behind what the app needs from it (3.4). App only calls Initialize after consent finishes.</summary>
    public interface IAdService
    {
        void Initialize();
        bool IsInterstitialReady { get; }
        bool IsRewardedReady { get; }

        /// <summary>Shows an interstitial; closed runs when it is gone, shown or not.</summary>
        void ShowInterstitial(Action closed);

        /// <summary>Shows a rewarded ad; finished gets true only when the player earned the reward.</summary>
        void ShowRewarded(Action<bool> finished);

        void ShowBanner();
        void HideBanner();

        /// <summary>Every paid impression, of every format: MAX's OnAdRevenuePaidEvent (3.5).</summary>
        event Action<AdRevenue>? RevenuePaid;
    }

    /// <summary>Until Google's consent form is wired in with MAX (3.2, 3.3): finishes at once.</summary>
    public sealed class StandInConsent : IConsentFlow
    {
        public void Request(Action finished) => finished();
    }

    /// <summary>
    /// Until AppLovin MAX is wired in (3.3): no SDK and no network. Ads are "ready" once initialized and close at
    /// once; a rewarded one always rewards. It lets every placement run, in the Editor and in development builds.
    /// </summary>
    public sealed class StandInAds : IAdService
    {
        bool _initialized;

        public void Initialize() => _initialized = true;

        public bool IsInterstitialReady => _initialized;

        public bool IsRewardedReady => _initialized;

        public void ShowInterstitial(Action closed) => closed();

        public void ShowRewarded(Action<bool> finished) => finished(true);

        public void ShowBanner()
        {
        }

        public void HideBanner()
        {
        }

        /// <summary>No ad network, so nothing is ever paid.</summary>
        public event Action<AdRevenue>? RevenuePaid
        {
            add { }
            remove { }
        }
    }

    /// <summary>The waits between retries of a failed ad load: 2, 4, 8… seconds, up to 64, for the MAX wrapper (3.4).</summary>
    public static class RetrySchedule
    {
        const float MaxSeconds = 64f;

        public static float DelayAfter(int failures) =>
            failures <= 0 ? 0f : failures >= 6 ? MaxSeconds : (float)Math.Pow(2, failures);
    }
}
