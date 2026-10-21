#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class ConsentResultTests
    {
        [Test]
        public void Until_Google_s_form_is_in_nothing_may_be_stored()
        {
            var unknown = new StandInConsent();
            ConsentResult? result = null;
            unknown.Request(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsFalse(result!.AnalyticsStorage);
            Assert.IsFalse(result.AdStorage);
            Assert.IsFalse(result.AdUserData);
            Assert.IsFalse(result.AdPersonalization);
            Assert.IsFalse(unknown.PrivacyOptionsRequired);
        }

        [Test]
        public void Consent_result_says_unknown_until_the_flow_knows()
        {
            var parameters = ConsentResult.Unknown.EventParameters();

            Assert.AreEqual("unknown", parameters["gdpr_applies"]);
            Assert.AreEqual("unknown", parameters["att_status"]);
        }

        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void Consent_result_says_whether_GDPR_applies(bool applies, string expected)
        {
            var result = new ConsentResult(applies, "unavailable", analyticsStorage: true, adStorage: true, adUserData: true, adPersonalization: true);

            Assert.AreEqual(expected, result.EventParameters()["gdpr_applies"]);
            Assert.AreEqual("unavailable", result.EventParameters()["att_status"]);
        }
    }
}
