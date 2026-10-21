#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class AllocationTallyTests
    {
        [Test]
        public void Counts_the_frames_that_allocated_and_the_largest()
        {
            var tally = new AllocationTally();
            tally.Add(0, ownRefresh: false);
            tally.Add(128, ownRefresh: false);
            tally.Add(64, ownRefresh: false);
            tally.EndWindow();

            Assert.AreEqual(2, tally.Frames);
            Assert.AreEqual(128, tally.LargestBytes);
        }

        [Test]
        public void Leaves_out_the_overlays_own_refresh()
        {
            var tally = new AllocationTally();
            tally.Add(4096, ownRefresh: true);
            tally.EndWindow();

            Assert.AreEqual(0, tally.Frames);
            Assert.AreEqual(0, tally.LargestBytes);
        }

        [Test]
        public void Shows_a_window_only_once_it_ends()
        {
            var tally = new AllocationTally();
            tally.Add(256, ownRefresh: false);

            Assert.AreEqual(0, tally.Frames);
        }

        [Test]
        public void Each_window_starts_afresh()
        {
            var tally = new AllocationTally();
            tally.Add(256, ownRefresh: false);
            tally.EndWindow();
            tally.EndWindow();

            Assert.AreEqual(0, tally.Frames);
            Assert.AreEqual(0, tally.LargestBytes);
        }
    }
}
