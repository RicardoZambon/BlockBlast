#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.Game.Tests
{
    public class SafeAreaTests
    {
        [Test]
        public void Anchors_cover_the_safe_area()
        {
            // A 1080x2400 phone with a 96 px notch band at the bottom and 88 px at the top.
            SafeArea.Anchors(new Rect(0, 96, 1080, 2216), new Vector2(1080, 2400), out var min, out var max);

            Assert.AreEqual(0f, min.x, 1e-5f);
            Assert.AreEqual(0.04f, min.y, 1e-5f);
            Assert.AreEqual(1f, max.x, 1e-5f);
            Assert.AreEqual(2312f / 2400f, max.y, 1e-5f);
        }

        [Test]
        public void The_full_screen_and_a_zero_screen_give_full_anchors()
        {
            SafeArea.Anchors(new Rect(0, 0, 1080, 2400), new Vector2(1080, 2400), out var min, out var max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);

            SafeArea.Anchors(new Rect(0, 0, 0, 0), Vector2.zero, out min, out max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
