#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// Tuning values for the piece generator. Public fields so Unity's JsonUtility and the
    /// Remote Config loader can fill them.
    /// </summary>
    [Serializable]
    public sealed class GeneratorSettings
    {
        public string Name = "custom";
        public FitGuarantee FitGuarantee = FitGuarantee.AtLeastOne;
        /// <summary>Category weights by game progress, sorted by FromPlacement; the first band starts at 0.</summary>
        public DifficultyBand[] Bands = { new DifficultyBand(0, 0, 35, 45, 20) };
        /// <summary>Until this placement, a slot may prefer a piece that can complete a line.</summary>
        public int HelpfulUntilPlacement = 15;
        /// <summary>Chance (0-100) that a slot uses that preference while it applies.</summary>
        public int HelpfulChancePercent = 50;
        /// <summary>After this many quick losses in a row, the next game starts with mercy weights. 0 turns mercy off.</summary>
        public int MercyAfterQuickLosses = 2;
        /// <summary>A game that ends before this many placements counts as a quick loss.</summary>
        public int QuickLossPlacements = 20;
        /// <summary>How many placements of a mercy game use the mercy weights.</summary>
        public int MercyPlacements = 30;
        public DifficultyBand Mercy = new DifficultyBand(0, 5, 55, 35, 5);
        /// <summary>Re-deal attempts for WholeTray before falling back to AtLeastOne.</summary>
        public int MaxWholeTrayAttempts = 20;

        public DifficultyBand BandFor(int placements)
        {
            DifficultyBand band = Bands[0];
            for (int i = 1; i < Bands.Length && Bands[i].FromPlacement <= placements; i++) band = Bands[i];
            return band;
        }

        /// <summary>Throws if these settings can't work (for example, a mistyped Remote Config value).</summary>
        public void Validate()
        {
            if (Bands == null || Bands.Length == 0) throw new ArgumentException("At least one difficulty band is required.");
            if (Bands[0].FromPlacement != 0) throw new ArgumentException("The first difficulty band must start at placement 0.");
            for (int i = 0; i < Bands.Length; i++)
            {
                ValidateBand(Bands[i], $"Bands[{i}]");
                if (i > 0 && Bands[i].FromPlacement <= Bands[i - 1].FromPlacement)
                    throw new ArgumentException("Difficulty bands must be sorted by FromPlacement.");
            }
            ValidateBand(Mercy, nameof(Mercy));
            if (HelpfulChancePercent < 0 || HelpfulChancePercent > 100)
                throw new ArgumentException("HelpfulChancePercent must be between 0 and 100.");
            if (MaxWholeTrayAttempts < 1) throw new ArgumentException("MaxWholeTrayAttempts must be at least 1.");
        }

        static void ValidateBand(DifficultyBand band, string name)
        {
            if (band == null) throw new ArgumentException($"{name} is missing.");
            if (band.Tiny < 0 || band.Small < 0 || band.Medium < 0 || band.Large < 0)
                throw new ArgumentException($"{name} has a negative weight.");
            if (band.Tiny + band.Small + band.Medium + band.Large == 0)
                throw new ArgumentException($"{name} has no positive weight.");
        }

        /// <summary>Easier: every fresh tray can be placed, more small pieces, longer helpful phase.</summary>
        public static GeneratorSettings Gentle() => new GeneratorSettings
        {
            Name = "gentle",
            FitGuarantee = FitGuarantee.WholeTray,
            Bands = new[]
            {
                new DifficultyBand(0, 0, 45, 42, 13),
                new DifficultyBand(40, 0, 38, 44, 18),
                new DifficultyBand(120, 0, 30, 45, 25),
            },
            HelpfulUntilPlacement = 30,
            HelpfulChancePercent = 70,
        };

        /// <summary>The default for new players.</summary>
        public static GeneratorSettings Baseline() => new GeneratorSettings
        {
            Name = "baseline",
            Bands = new[]
            {
                new DifficultyBand(0, 0, 35, 45, 20),
                new DifficultyBand(30, 0, 28, 45, 27),
                new DifficultyBand(90, 0, 22, 43, 35),
            },
        };

        /// <summary>Harder: more large pieces sooner, no helpful phase.</summary>
        public static GeneratorSettings Hard() => new GeneratorSettings
        {
            Name = "hard",
            Bands = new[]
            {
                new DifficultyBand(0, 0, 25, 45, 30),
                new DifficultyBand(25, 0, 20, 42, 38),
                new DifficultyBand(70, 0, 15, 40, 45),
            },
            HelpfulUntilPlacement = 0,
        };

        public static GeneratorSettings ForProfile(string name)
        {
            switch (name)
            {
                case "gentle": return Gentle();
                case "baseline": return Baseline();
                case "hard": return Hard();
                default: throw new ArgumentException($"Unknown generator profile '{name}'.", nameof(name));
            }
        }
    }
}
