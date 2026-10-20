#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class DailyShareTests
    {
        const int October20 = 20746;

        [Test]
        public void The_shared_text_has_the_date_the_score_and_the_streak()
        {
            Assert.AreEqual("Block Puzzle Daily 2026-10-20: 1240 points, 3-day streak", DailyShare.Text("Block Puzzle", October20, 1240, 3));
        }

        [Test]
        public void Without_a_streak_the_text_leaves_it_out()
        {
            Assert.AreEqual("Block Puzzle Daily 2026-10-20: 90 points", DailyShare.Text("Block Puzzle", October20, 90, 0));
        }
    }
}
