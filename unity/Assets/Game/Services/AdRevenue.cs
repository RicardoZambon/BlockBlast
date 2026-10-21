#nullable enable
using System.Collections.Generic;

namespace BlockPuzzle.Game
{
    /// <summary>One paid ad impression, as MAX's OnAdRevenuePaidEvent reports it (3.5).</summary>
    public readonly struct AdRevenue
    {
        public AdRevenue(string? network, string format, string? adUnit, double revenue)
        {
            Network = network;
            Format = format;
            AdUnit = adUnit;
            Revenue = revenue;
        }

        public string? Network { get; }

        /// <summary>As MAX names it: "INTER", "REWARDED" or "BANNER".</summary>
        public string Format { get; }

        public string? AdUnit { get; }

        /// <summary>In US dollars. MAX reports -1 when it doesn't know.</summary>
        public double Revenue { get; }
    }

    /// <summary>The master plan's <c>ad_impression</c> event (3.1's table and 3.5).</summary>
    public static class AdRevenueEvent
    {
        public const string Name = "ad_impression";

        static readonly IReadOnlyDictionary<string, object> None = new Dictionary<string, object>();

        /// <summary>False for a revenue that would corrupt the totals: negative (MAX's unknown), NaN or infinite.</summary>
        public static bool TryParameters(AdRevenue impression, out IReadOnlyDictionary<string, object> parameters)
        {
            double revenue = impression.Revenue;
            if (double.IsNaN(revenue) || double.IsInfinity(revenue) || revenue < 0)
            {
                parameters = None;
                return false;
            }
            parameters = new Dictionary<string, object>
            {
                ["ad_platform"] = "AppLovin",
                ["ad_source"] = OrUnknown(impression.Network),
                ["ad_format"] = impression.Format,
                ["ad_unit_name"] = OrUnknown(impression.AdUnit),
                ["value"] = revenue,
                ["currency"] = "USD",
            };
            return true;
        }

        static string OrUnknown(string? name) => string.IsNullOrEmpty(name) ? "unknown" : name!;
    }
}
