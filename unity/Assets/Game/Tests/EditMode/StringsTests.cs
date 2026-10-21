#nullable enable
using System.Linq;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class StringsTests
    {
        [Test]
        public void Every_key_has_english_text()
        {
            Assert.Greater(Strings.Keys.Count(), 30);
            foreach (string key in Strings.Keys)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(Strings.Get(key)), key);
                Assert.AreNotEqual(key, Strings.Get(key), key);
            }
        }

        [Test]
        public void A_missing_key_shows_itself() => Assert.AreEqual("no.such.key", Strings.Get("no.such.key"));

        [Test]
        public void Numbers_are_written_the_same_way_everywhere()
        {
            Assert.AreEqual("Best 12345", Strings.Format("hud.best", 12345));
            Assert.AreEqual("Score 1240\nBest 2000", Strings.Format("game_over.scores", 1240, 2000));
        }
    }
}
