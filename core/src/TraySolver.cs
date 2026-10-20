#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Checks whether pieces can all be placed in some order, counting line clears in between.</summary>
    public static class TraySolver
    {
        /// <param name="pieces">At most three pieces; null entries (used tray slots) are skipped.</param>
        public static bool CanPlaceAll(Board board, PieceShape?[] pieces)
        {
            if (pieces == null) throw new ArgumentNullException(nameof(pieces));
            if (pieces.Length > PieceGenerator.TraySize) throw new ArgumentException("At most three pieces are supported.", nameof(pieces));

            int used = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null) used |= 1 << i;
            }
            return Solve(board, pieces, used);
        }

        static bool Solve(Board board, PieceShape?[] pieces, int used)
        {
            bool anyLeft = false;
            for (int i = 0; i < pieces.Length; i++)
            {
                if ((used & (1 << i)) != 0) continue;
                anyLeft = true;
                if (SameAsEarlierUnused(pieces, used, i)) continue; // same subtree as the earlier copy

                foreach (ulong placement in pieces[i]!.Placements)
                {
                    if (board.Fits(placement) && Solve(board.Place(placement).Board, pieces, used | (1 << i))) return true;
                }
            }
            return !anyLeft;
        }

        static bool SameAsEarlierUnused(PieceShape?[] pieces, int used, int index)
        {
            for (int j = 0; j < index; j++)
            {
                if ((used & (1 << j)) == 0 && ReferenceEquals(pieces[j], pieces[index])) return true;
            }
            return false;
        }
    }
}
