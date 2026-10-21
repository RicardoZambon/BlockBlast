#nullable enable
using System.Collections.Generic;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// What the consent flow found out (master plan 3.2): whether GDPR applies, the player's answer to Apple's tracking
    /// prompt, and the four switches of Firebase's consent mode, which analytics gets before it collects anything.
    /// </summary>
    public sealed class ConsentResult
    {
        public ConsentResult(bool? gdprApplies, string attStatus, bool analyticsStorage, bool adStorage, bool adUserData, bool adPersonalization)
        {
            GdprApplies = gdprApplies;
            AttStatus = attStatus;
            AnalyticsStorage = analyticsStorage;
            AdStorage = adStorage;
            AdUserData = adUserData;
            AdPersonalization = adPersonalization;
        }

        /// <summary>Nothing known, so nothing may be stored: the stand-in's answer until Google's form (UMP) is in.</summary>
        public static ConsentResult Unknown { get; } = new ConsentResult(null, "unknown", false, false, false, false);

        /// <summary>Whether GDPR applies where the player is; null until Google's form says.</summary>
        public bool? GdprApplies { get; }

        /// <summary>"authorized", "denied", "restricted" or "not_determined" on iOS; "unavailable" elsewhere; "unknown" until known.</summary>
        public string AttStatus { get; }

        public bool AnalyticsStorage { get; }
        public bool AdStorage { get; }
        public bool AdUserData { get; }
        public bool AdPersonalization { get; }

        /// <summary>The consent_result event's parameters (3.1's table).</summary>
        public IReadOnlyDictionary<string, object> EventParameters() => new Dictionary<string, object>
        {
            ["gdpr_applies"] = GdprApplies == null ? "unknown" : GdprApplies.Value ? "true" : "false",
            ["att_status"] = AttStatus,
        };
    }
}
