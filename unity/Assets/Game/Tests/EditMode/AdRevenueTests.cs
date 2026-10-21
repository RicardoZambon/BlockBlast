#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class AdRevenueTests
    {
        [Test]
        public void A_paid_impression_becomes_the_ad_impression_parameters()
        {
            Assert.IsTrue(AdRevenueEvent.TryParameters(new AdRevenue("ADMOB_BIDDING", "INTER", "inter_unit", 0.0125), out var parameters));

            Assert.AreEqual("AppLovin", parameters["ad_platform"]);
            Assert.AreEqual("ADMOB_BIDDING", parameters["ad_source"]);
            Assert.AreEqual("INTER", parameters["ad_format"]);
            Assert.AreEqual("inter_unit", parameters["ad_unit_name"]);
            Assert.AreEqual(0.0125, parameters["value"]);
            Assert.AreEqual("USD", parameters["currency"]);
            Assert.AreEqual(6, parameters.Count);
        }

        [TestCase(-1.0)]   // MAX's "unknown"
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void A_revenue_that_would_corrupt_the_totals_is_not_logged(double revenue)
        {
            Assert.IsFalse(AdRevenueEvent.TryParameters(new AdRevenue("APPLOVIN_NETWORK", "BANNER", "banner_unit", revenue), out _));
        }

        [Test]
        public void A_revenue_of_zero_is_logged()
        {
            Assert.IsTrue(AdRevenueEvent.TryParameters(new AdRevenue("APPLOVIN_NETWORK", "REWARDED", "rewarded_unit", 0.0), out var parameters));
            Assert.AreEqual(0.0, parameters["value"]);
        }

        [Test]
        public void Missing_names_are_logged_as_unknown()
        {
            Assert.IsTrue(AdRevenueEvent.TryParameters(new AdRevenue("", "BANNER", null, 0.001), out var parameters));

            Assert.AreEqual("unknown", parameters["ad_source"]);
            Assert.AreEqual("unknown", parameters["ad_unit_name"]);
        }
    }
}
