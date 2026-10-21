#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The master plan's Remote Config keys (3.1), read into the settings they tune. A key without a value, or with one
    /// that doesn't parse or that the engine rejects, keeps its default, so a typo in the console can't break a game.
    /// </summary>
    public sealed class RemoteTuning
    {
        static readonly Regex Sku = new Regex("^[a-z0-9][a-z0-9_.]*$");

        RemoteTuning(GeneratorSettings generator, ScoringSettings scoring, AdPolicySettings adPolicy, bool bannerEnabled,
            bool reviveEnabled, string removeAdsSku, int reviewPromptMinGames)
        {
            Generator = generator;
            Scoring = scoring;
            AdPolicy = adPolicy;
            BannerEnabled = bannerEnabled;
            ReviveEnabled = reviveEnabled;
            RemoveAdsSku = removeAdsSku;
            ReviewPromptMinGames = reviewPromptMinGames;
        }

        /// <summary>The defaults bundled in the app: how it plays offline, or before the first fetch.</summary>
        public static RemoteTuning Defaults { get; } = From(_ => null);

        /// <summary>For new Classic games: gen_profile, then gen_fit_guarantee, gen_helpful_until_placement and gen_mercy_after_quick_losses.</summary>
        public GeneratorSettings Generator { get; }

        /// <summary>For new Classic games: score_line_base and score_board_clear_bonus.</summary>
        public ScoringSettings Scoring { get; }

        /// <summary>ads_inter_enabled, ads_inter_grace_games and ads_inter_cooldown_s.</summary>
        public AdPolicySettings AdPolicy { get; }

        public bool BannerEnabled { get; }
        public bool ReviveEnabled { get; }

        /// <summary>The product the IAP wrapper sells (3.6), for price tests.</summary>
        public string RemoveAdsSku { get; }

        public int ReviewPromptMinGames { get; }

        /// <param name="value">A key's value as Remote Config holds it, or null when it has none.</param>
        public static RemoteTuning From(Func<string, string?> value)
        {
            string? profile = value("gen_profile");
            var generator = GeneratorSettings.ForProfile(profile != null && GeneratorSettings.ProfileNames.Contains(profile) ? profile : "baseline");
            if (TryFitGuarantee(value("gen_fit_guarantee"), out var fit)) generator = Checked(generator, g => g.FitGuarantee = fit);
            if (TryInt(value("gen_helpful_until_placement"), 0, 1000, out int helpful)) generator = Checked(generator, g => g.HelpfulUntilPlacement = helpful);
            if (TryInt(value("gen_mercy_after_quick_losses"), 0, 100, out int mercy)) generator = Checked(generator, g => g.MercyAfterQuickLosses = mercy);

            var scoring = new ScoringSettings();
            if (TryInt(value("score_line_base"), 0, int.MaxValue, out int lineBase)) scoring = Checked(scoring, s => s.LineBase = lineBase);
            if (TryInt(value("score_board_clear_bonus"), 0, int.MaxValue, out int bonus)) scoring = Checked(scoring, s => s.BoardClearBonus = bonus);

            var adPolicy = new AdPolicySettings();
            if (TryBool(value("ads_inter_enabled"), out bool interstitials)) adPolicy.InterstitialsEnabled = interstitials;
            if (TryInt(value("ads_inter_grace_games"), 0, 1000, out int grace)) adPolicy.GraceGames = grace;
            if (TryInt(value("ads_inter_cooldown_s"), 0, 86_400, out int cooldown)) adPolicy.CooldownSeconds = cooldown;

            string? sku = value("iap_remove_ads_sku");
            return new RemoteTuning(generator, scoring, adPolicy,
                TryBool(value("ads_banner_enabled"), out bool banner) ? banner : true,
                TryBool(value("ads_revive_enabled"), out bool revive) ? revive : true,
                sku != null && Sku.IsMatch(sku) ? sku : "remove_ads_299",
                TryInt(value("review_prompt_min_games"), 0, 1000, out int minGames) ? minGames : 5);
        }

        /// <summary>The settings with one change, or unchanged if the engine's checks reject it.</summary>
        static GeneratorSettings Checked(GeneratorSettings settings, Action<GeneratorSettings> change)
        {
            var changed = settings.Clone();
            change(changed);
            try
            {
                changed.Validate();
                return changed;
            }
            catch (ArgumentException)
            {
                return settings;
            }
        }

        static ScoringSettings Checked(ScoringSettings settings, Action<ScoringSettings> change)
        {
            var changed = settings.Clone();
            change(changed);
            try
            {
                changed.Validate();
                return changed;
            }
            catch (ArgumentException)
            {
                return settings;
            }
        }

        static bool TryInt(string? text, int min, int max, out int result) =>
            int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result) && result >= min && result <= max;

        /// <summary>"true" or "false", in any case, as the Remote Config console writes them.</summary>
        static bool TryBool(string? text, out bool result)
        {
            result = string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
            return result || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase);
        }

        static bool TryFitGuarantee(string? text, out FitGuarantee result)
        {
            switch (text)
            {
                case "none": result = FitGuarantee.None; return true;
                case "at_least_one": result = FitGuarantee.AtLeastOne; return true;
                case "whole_tray": result = FitGuarantee.WholeTray; return true;
                default: result = default; return false;
            }
        }
    }
}
