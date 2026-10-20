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
    }
}
