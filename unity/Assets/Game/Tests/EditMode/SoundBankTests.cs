#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class SoundBankTests
    {
        [TestCase(Sound.Place)]
        [TestCase(Sound.Clear)]
        [TestCase(Sound.BoardClear)]
        public void Every_sound_has_a_short_clip(Sound sound)
        {
            var clip = SoundBank.Clip(sound);

            Assert.IsNotNull(clip);
            Assert.Greater(clip!.samples, 0);
            Assert.Less(clip.length, 1f);
        }

        [Test]
        public void No_sound_has_no_clip() => Assert.IsNull(SoundBank.Clip(Sound.None));
    }
}
