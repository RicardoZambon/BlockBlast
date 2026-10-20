#nullable enable
namespace BlockPuzzle.Core
{
    /// <summary>What happened when a piece was placed.</summary>
    public readonly struct PlaceResult
    {
        public readonly Board Board;
        public readonly int Lines;
        /// <summary>The cells that were cleared, so the view can animate exactly those.</summary>
        public readonly ulong ClearedMask;

        public PlaceResult(Board board, int lines, ulong clearedMask)
        {
            Board = board;
            Lines = lines;
            ClearedMask = clearedMask;
        }

        public bool BoardCleared => Lines > 0 && Board.IsEmpty;
    }
}
