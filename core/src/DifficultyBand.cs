#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// Category weights that apply from a given placement onward. Weights are relative integers;
    /// they don't need to add up to 100.
    /// </summary>
    [Serializable]
    public sealed class DifficultyBand
    {
        public int FromPlacement;
        public int Tiny;
        public int Small;
        public int Medium;
        public int Large;

        public DifficultyBand() { }

        public DifficultyBand(int fromPlacement, int tiny, int small, int medium, int large)
        {
            FromPlacement = fromPlacement;
            Tiny = tiny;
            Small = small;
            Medium = medium;
            Large = large;
        }

        public int WeightOf(PieceCategory category)
        {
            switch (category)
            {
                case PieceCategory.Tiny: return Tiny;
                case PieceCategory.Small: return Small;
                case PieceCategory.Medium: return Medium;
                case PieceCategory.Large: return Large;
                default: return 0;
            }
        }
    }
}
