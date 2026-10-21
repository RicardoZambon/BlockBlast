#nullable enable

namespace BlockPuzzle.Game
{
    /// <summary>Text size limits for the 1080×1920 reference canvas, where a 360 dp phone is about 3 units per dp.</summary>
    public static class Readability
    {
        /// <summary>About 12 sp: no text, not even text that shrinks to fit, gets smaller than this.</summary>
        public const int MinTextSize = 36;
    }
}
