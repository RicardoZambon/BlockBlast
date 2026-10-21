#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The prototype's plain colours.</summary>
    public static class Palette
    {
        public static readonly Color Background = Hex(0x1E2230);
        public static readonly Color Empty = Hex(0x2E3446);
        public static readonly Color Filled = Hex(0x4FA3F7);
        public static readonly Color Ghost = new Color(Filled.r, Filled.g, Filled.b, 0.45f);
        public static readonly Color Clearing = Hex(0xF7D154);

        /// <summary>Buttons: darker than Filled, so their white labels pass 4.5:1.</summary>
        public static readonly Color Button = Hex(0x2A6BC9);

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
