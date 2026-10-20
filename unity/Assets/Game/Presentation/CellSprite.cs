#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The one sprite the prototype draws: a white square one world unit wide, made at runtime so there is no image asset.</summary>
    public static class CellSprite
    {
        /// <summary>Squares are drawn at this fraction of a cell, so the gaps between them show.</summary>
        public const float Fill = 0.9f;

        static Sprite? _square;

        public static Sprite Square
        {
            get
            {
                // Texture2D.whiteTexture is 4x4 pixels; 4 pixels per unit makes the sprite 1x1.
                if (_square == null) _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                return _square;
            }
        }

        static Sprite? _circle;

        /// <summary>A white disc one unit across, with a one-pixel soft edge: the tutorial's hand until there is art.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                float radius = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - radius, dy = y + 0.5f - radius;
                        float edge = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(edge * 255));
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, makeNoLongerReadable: true);
                _circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                return _circle;
            }
        }
    }
}
