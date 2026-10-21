#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class RetryScheduleTests
    {
        [TestCase(1, 2f)]
        [TestCase(2, 4f)]
        [TestCase(3, 8f)]
        [TestCase(5, 32f)]
        [TestCase(6, 64f)]
        [TestCase(7, 64f)]
        [TestCase(40, 64f)]
        public void Each_failed_load_doubles_the_wait_up_to_64_seconds(int failures, float seconds) =>
            Assert.AreEqual(seconds, RetrySchedule.DelayAfter(failures));

        [Test]
        public void No_failure_means_no_wait() => Assert.AreEqual(0f, RetrySchedule.DelayAfter(0));
    }
}
