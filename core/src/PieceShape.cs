#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// One piece shape. Pieces never rotate, so each orientation is its own shape.
    /// Every legal position on the board is precomputed as a cell mask.
    /// </summary>
    public sealed class PieceShape
    {
        readonly ulong[] _placements;
        readonly int[] _anchors; // bit index of the top-left corner of each placement

        public string Id { get; }
        public PieceCategory Category { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; }
        /// <summary>The shape with its top-left corner at row 0, column 0.</summary>
        public ulong BaseMask { get; }

        PieceShape(string id, PieceCategory category, int width, int height, int cellCount, ulong baseMask)
        {
            Id = id;
            Category = category;
            Width = width;
            Height = height;
            CellCount = cellCount;
            BaseMask = baseMask;

            int count = (Board.Size - height + 1) * (Board.Size - width + 1);
            _placements = new ulong[count];
            _anchors = new int[count];
            int i = 0;
            for (int row = 0; row + height <= Board.Size; row++)
            {
                for (int col = 0; col + width <= Board.Size; col++)
                {
                    int anchor = Board.BitIndex(row, col);
                    _placements[i] = baseMask << anchor;
                    _anchors[i] = anchor;
                    i++;
                }
            }
        }

        /// <summary>Every position where the shape stays inside the board, as cell masks.</summary>
        public ReadOnlySpan<ulong> Placements => _placements;

        public int PlacementCount => _placements.Length;

        public int AnchorRow(int placementIndex) => _anchors[placementIndex] / Board.Size;

        public int AnchorCol(int placementIndex) => _anchors[placementIndex] % Board.Size;

        /// <summary>The mask with the top-left corner at (row, col); false if the piece would leave the board.</summary>
        public bool TryGetMask(int row, int col, out ulong mask)
        {
            // Unsigned compare: negative values become huge, and nothing can overflow near int.MaxValue.
            if ((uint)row > (uint)(Board.Size - Height) || (uint)col > (uint)(Board.Size - Width))
            {
                mask = 0;
                return false;
            }
            mask = BaseMask << Board.BitIndex(row, col);
            return true;
        }

        public bool FitsAnywhere(Board board)
        {
            foreach (ulong placement in _placements)
            {
                if (board.Fits(placement)) return true;
            }
            return false;
        }

        /// <summary>True if some placement on this board would complete at least one line.</summary>
        public bool CanClearLine(Board board)
        {
            foreach (ulong placement in _placements)
            {
                if (board.Fits(placement) && board.Place(placement).Lines > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a shape from text art: '#' = cell, '.' = gap. The art must be tight:
        /// no empty first or last row or column.
        /// </summary>
        public static PieceShape FromRows(string id, PieceCategory category, params string[] rows)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("An id is required.", nameof(id));
            if (rows == null || rows.Length == 0) throw new ArgumentException("At least one row is required.", nameof(rows));

            int height = rows.Length;
            int width = rows[0].Length;
            if (height > Board.Size || width == 0 || width > Board.Size)
                throw new ArgumentException("The shape must fit on the board.", nameof(rows));

            ulong mask = 0;
            int cells = 0;
            for (int r = 0; r < height; r++)
            {
                if (rows[r].Length != width) throw new ArgumentException($"Row {r} has a different length.", nameof(rows));
                for (int c = 0; c < width; c++)
                {
                    char ch = rows[r][c];
                    if (ch == '#')
                    {
                        mask |= 1UL << Board.BitIndex(r, c);
                        cells++;
                    }
                    else if (ch != '.')
                    {
                        throw new ArgumentException($"Unexpected character '{ch}'.", nameof(rows));
                    }
                }
            }

            if (!HasCell(mask, Board.RowMask(0)) || !HasCell(mask, Board.RowMask(height - 1)) ||
                !HasCell(mask, Board.ColumnMask(0)) || !HasCell(mask, Board.ColumnMask(width - 1)))
                throw new ArgumentException("The art must not have an empty edge row or column.", nameof(rows));

            return new PieceShape(id, category, width, height, cells, mask);
        }

        static bool HasCell(ulong mask, ulong line) => (mask & line) != 0;

        public override string ToString() => Id;
    }
}
