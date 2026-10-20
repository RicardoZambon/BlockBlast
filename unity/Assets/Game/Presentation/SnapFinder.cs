#nullable enable
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>Where a dragged piece would land: the nearest legal anchor less than one cell away.</summary>
    public static class SnapFinder
    {
        /// <summary>How far, in cells, a piece may be from a legal spot and still snap to it.</summary>
        public const float MaxSnapDistance = 1f;

        /// <param name="topLeftRow">Where the dragged piece's top-left corner is, in cells from the board's top-left corner.</param>
        /// <param name="topLeftCol">The same, across.</param>
        /// <param name="onlyRow">With onlyCol, the one anchor allowed, as in the tutorial; -1 allows any.</param>
        /// <returns>False when no legal spot is close enough. Allocates nothing.</returns>
        public static bool TryFind(GameSession game, int slot, float topLeftRow, float topLeftCol, out int row, out int col,
            int onlyRow = -1, int onlyCol = -1)
        {
            int roundedRow = Mathf.FloorToInt(topLeftRow + 0.5f);
            int roundedCol = Mathf.FloorToInt(topLeftCol + 0.5f);
            float best = MaxSnapDistance * MaxSnapDistance;
            bool found = false;
            row = -1;
            col = -1;

            // Upper-left candidates come first and a tie never replaces them, so ties go up and left.
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int r = roundedRow + dr;
                    int c = roundedCol + dc;
                    if (onlyRow >= 0 && (r != onlyRow || c != onlyCol)) continue;
                    if (!game.CanPlace(slot, r, c)) continue;
                    float distanceSquared = (r - topLeftRow) * (r - topLeftRow) + (c - topLeftCol) * (c - topLeftCol);
                    if (distanceSquared >= best) continue;
                    best = distanceSquared;
                    row = r;
                    col = c;
                    found = true;
                }
            }
            return found;
        }
    }
}
