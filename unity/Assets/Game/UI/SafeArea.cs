#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The safe area (clear of notches, rounded corners and system bars) as anchors on the screen.</summary>
    public static class SafeArea
    {
        /// <summary>The safe area as fractions of the screen. With no screen, or no safe area, the whole screen.</summary>
        public static void Anchors(Rect safeArea, Vector2 screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0 || safeArea.width <= 0 || safeArea.height <= 0)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }
            anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        }
    }
}
