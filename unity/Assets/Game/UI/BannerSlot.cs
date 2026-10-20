#nullable enable
namespace BlockPuzzle.Game
{
    /// <summary>
    /// The band at the top of the game screen kept free for the banner ad from day one, so turning ads on or off never
    /// moves the board: 50 dp on phones, 90 dp on screens at least 728 dp wide (the standard banner and leaderboard heights).
    /// </summary>
    public static class BannerSlot
    {
        public const float PhoneDp = 50f;
        public const float TabletDp = 90f;
        public const float TabletMinWidthDp = 728f;

        const float BaselineDpi = 160f;   // 1 dp is 1 px at 160 dpi

        /// <param name="dpi">Screen.dpi; 0 or less (unknown) counts as 160.</param>
        public static float HeightPixels(float dpi, float screenWidthPixels)
        {
            if (dpi <= 0) dpi = BaselineDpi;
            float widthDp = screenWidthPixels * BaselineDpi / dpi;
            float heightDp = widthDp >= TabletMinWidthDp ? TabletDp : PhoneDp;
            return heightDp * dpi / BaselineDpi;
        }
    }
}
