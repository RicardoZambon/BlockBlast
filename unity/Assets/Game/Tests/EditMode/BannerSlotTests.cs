#nullable enable
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class BannerSlotTests
    {
        [Test]
        public void A_phone_gets_a_50_dp_banner()
        {
            // 1080 px at 440 dpi is 393 dp wide: a phone.
            Assert.AreEqual(50f * 440f / 160f, BannerSlot.HeightPixels(440f, 1080f), 1e-3f);
        }

        [Test]
        public void A_tablet_gets_a_90_dp_banner()
        {
            // 1536 px at 264 dpi is 931 dp wide: at least 728 dp, so a tablet.
            Assert.AreEqual(90f * 264f / 160f, BannerSlot.HeightPixels(264f, 1536f), 1e-3f);
        }

        [Test]
        public void An_unknown_dpi_counts_as_160()
        {
            Assert.AreEqual(50f, BannerSlot.HeightPixels(0f, 600f), 1e-3f);
        }
    }
}
