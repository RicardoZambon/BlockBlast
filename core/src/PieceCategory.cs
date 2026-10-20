#nullable enable
namespace BlockPuzzle.Core
{
    /// <summary>Size groups that the generator weights against each other.</summary>
    public enum PieceCategory
    {
        /// <summary>A single cell. Only dealt by mercy weights or as a last resort.</summary>
        Tiny = 0,
        /// <summary>2-3 cells.</summary>
        Small = 1,
        /// <summary>4 cells.</summary>
        Medium = 2,
        /// <summary>5-9 cells.</summary>
        Large = 3,
    }
}
