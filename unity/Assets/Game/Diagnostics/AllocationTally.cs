#nullable enable

namespace BlockPuzzle.Game
{
    /// <summary>
    /// For the development overlay: over each window, how many frames allocated managed memory and the most one did, so
    /// "no allocations while dragging" can be read on a phone without the Profiler. A frame in which the overlay refreshed
    /// its own text is left out, since that allocates by design.
    /// </summary>
    public sealed class AllocationTally
    {
        int _frames;
        long _largest;

        /// <summary>Frames that allocated in the last window that ended.</summary>
        public int Frames { get; private set; }

        /// <summary>The most one frame allocated in the last window that ended, in bytes.</summary>
        public long LargestBytes { get; private set; }

        /// <param name="bytes">What one frame allocated on the managed heap.</param>
        /// <param name="ownRefresh">True for a frame in which the overlay changed its text.</param>
        public void Add(long bytes, bool ownRefresh)
        {
            if (ownRefresh || bytes <= 0) return;
            _frames++;
            if (bytes > _largest) _largest = bytes;
        }

        public void EndWindow()
        {
            Frames = _frames;
            LargestBytes = _largest;
            _frames = 0;
            _largest = 0;
        }
    }
}
