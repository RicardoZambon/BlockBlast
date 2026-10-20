#nullable enable
using System;
using System.Text;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// The 8x8 board, stored in one 64-bit integer. Bit index = row * 8 + col; row 0 is the top row.
    /// </summary>
    public readonly struct Board : IEquatable<Board>
    {
        public const int Size = 8;

        // 8 row masks followed by 8 column masks.
        static readonly ulong[] LineMasks = BuildLineMasks();

        public readonly ulong Cells;

        public Board(ulong cells) { Cells = cells; }

        public static Board Empty => default;

        public bool IsEmpty => Cells == 0;

        public static int BitIndex(int row, int col) => row * Size + col;

        public bool IsFilled(int row, int col)
        {
            if ((uint)row >= Size) throw new ArgumentOutOfRangeException(nameof(row));
            if ((uint)col >= Size) throw new ArgumentOutOfRangeException(nameof(col));
            return (Cells & (1UL << BitIndex(row, col))) != 0;
        }

        public bool Fits(ulong pieceMask) => (Cells & pieceMask) == 0;

        /// <summary>True if a row or column is full. Play never leaves one behind, so a saved board with one is corrupt.</summary>
        public bool HasFullLine
        {
            get
            {
                foreach (ulong line in LineMasks)
                {
                    if ((Cells & line) == line) return true;
                }
                return false;
            }
        }

        /// <summary>Adds the piece, then clears every full row and column at the same time.</summary>
        public PlaceResult Place(ulong pieceMask)
        {
            if (!Fits(pieceMask)) throw new InvalidOperationException("The piece overlaps filled cells.");

            ulong filled = Cells | pieceMask;
            ulong cleared = 0;
            int lines = 0;
            foreach (ulong line in LineMasks)
            {
                if ((filled & line) == line)
                {
                    cleared |= line;
                    lines++;
                }
            }
            return new PlaceResult(new Board(filled & ~cleared), lines, cleared);
        }

        /// <summary>Builds a board from 8 rows of 8 characters: '#' = filled, '.' = empty.</summary>
        public static Board Parse(params string[] rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (rows.Length != Size) throw new ArgumentException($"Expected {Size} rows, got {rows.Length}.", nameof(rows));

            ulong cells = 0;
            for (int r = 0; r < Size; r++)
            {
                string row = rows[r];
                if (row == null || row.Length != Size) throw new ArgumentException($"Row {r} must have {Size} characters.", nameof(rows));
                for (int c = 0; c < Size; c++)
                {
                    char ch = row[c];
                    if (ch == '#') cells |= 1UL << BitIndex(r, c);
                    else if (ch != '.') throw new ArgumentException($"Unexpected character '{ch}' at row {r}, column {c}.", nameof(rows));
                }
            }
            return new Board(cells);
        }

        public override string ToString()
        {
            var text = new StringBuilder(Size * (Size + 1));
            for (int r = 0; r < Size; r++)
            {
                if (r > 0) text.Append('\n');
                for (int c = 0; c < Size; c++) text.Append(IsFilled(r, c) ? '#' : '.');
            }
            return text.ToString();
        }

        public bool Equals(Board other) => Cells == other.Cells;
        public override bool Equals(object? obj) => obj is Board other && Equals(other);
        public override int GetHashCode() => Cells.GetHashCode();
        public static bool operator ==(Board left, Board right) => left.Equals(right);
        public static bool operator !=(Board left, Board right) => !left.Equals(right);

        static ulong[] BuildLineMasks()
        {
            var masks = new ulong[2 * Size];
            for (int i = 0; i < Size; i++)
            {
                masks[i] = 0xFFUL << (i * Size);              // row i
                masks[Size + i] = 0x0101010101010101UL << i;  // column i
            }
            return masks;
        }
    }
}
