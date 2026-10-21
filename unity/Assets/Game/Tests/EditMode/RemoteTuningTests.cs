#nullable enable
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class RemoteTuningTests
    {
        static RemoteTuning With(params (string key, string value)[] values)
        {
            var map = values.ToDictionary(v => v.key, v => v.value);
            return RemoteTuning.From(key => map.TryGetValue(key, out var value) ? value : null);
        }

        /// <summary>Every value a tuning holds, so two tunings can be compared at once.</summary>
        static string Describe(RemoteTuning t) =>
            $"{t.Generator.Name}|{t.Generator.FitGuarantee}|{t.Generator.HelpfulUntilPlacement}|{t.Generator.MercyAfterQuickLosses}|" +
            $"{t.Scoring.LineBase}|{t.Scoring.BoardClearBonus}|{t.AdPolicy.InterstitialsEnabled}|{t.AdPolicy.GraceGames}|" +
            $"{t.AdPolicy.CooldownSeconds}|{t.BannerEnabled}|{t.ReviveEnabled}|{t.RemoveAdsSku}|{t.ReviewPromptMinGames}";

        [Test]
        public void Without_values_it_plays_with_the_master_plan_s_defaults()
        {
            Assert.AreEqual("baseline|AtLeastOne|15|2|10|300|True|3|90|True|True|remove_ads_299|5", Describe(RemoteTuning.Defaults));
        }

        [Test]
        public void Every_key_is_read()
        {
            var tuning = With(("gen_profile", "hard"), ("gen_fit_guarantee", "whole_tray"), ("gen_helpful_until_placement", "8"),
                ("gen_mercy_after_quick_losses", "0"), ("score_line_base", "20"), ("score_board_clear_bonus", "500"),
                ("ads_inter_enabled", "false"), ("ads_inter_grace_games", "5"), ("ads_inter_cooldown_s", "120"),
                ("ads_banner_enabled", "FALSE"), ("ads_revive_enabled", "false"), ("iap_remove_ads_sku", "remove_ads_399"),
                ("review_prompt_min_games", "8"));

            Assert.AreEqual("hard|WholeTray|8|0|20|500|False|5|120|False|False|remove_ads_399|8", Describe(tuning));
        }

        [Test]
        public void A_profile_brings_its_own_settings()
        {
            var gentle = With(("gen_profile", "gentle")).Generator;

            Assert.AreEqual("gentle", gentle.Name);
            Assert.AreEqual(FitGuarantee.WholeTray, gentle.FitGuarantee);
            Assert.AreEqual(30, gentle.HelpfulUntilPlacement);
        }

        [TestCase("gen_profile", "impossible")]
        [TestCase("gen_fit_guarantee", "always")]
        [TestCase("gen_helpful_until_placement", "-1")]
        [TestCase("gen_helpful_until_placement", "fifteen")]
        [TestCase("gen_mercy_after_quick_losses", "-3")]
        [TestCase("score_line_base", "-10")]
        [TestCase("score_line_base", "2000000")]   // over the engine's limit
        [TestCase("score_board_clear_bonus", "1.5")]
        [TestCase("ads_inter_enabled", "yes")]
        [TestCase("ads_inter_grace_games", "-1")]
        [TestCase("ads_inter_cooldown_s", "")]
        [TestCase("ads_banner_enabled", "1")]
        [TestCase("ads_revive_enabled", "maybe")]
        [TestCase("iap_remove_ads_sku", "")]
        [TestCase("iap_remove_ads_sku", "Remove ads!")]
        [TestCase("review_prompt_min_games", "-2")]
        public void A_bad_value_keeps_its_default(string key, string value)
        {
            Assert.AreEqual(Describe(RemoteTuning.Defaults), Describe(With((key, value))));
        }

        [Test]
        public void One_bad_value_keeps_the_others()
        {
            var tuning = With(("score_line_base", "oops"), ("score_board_clear_bonus", "500"));

            Assert.AreEqual(10, tuning.Scoring.LineBase);
            Assert.AreEqual(500, tuning.Scoring.BoardClearBonus);
        }
    }
}
