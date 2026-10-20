#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>A stretched UI panel that covers only the safe area, and follows it when it changes (a fold, a new window).</summary>
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        bool _applied;
        Rect _appliedArea;
        Vector2 _appliedScreen;

        public static RectTransform Create(Transform canvas)
        {
            var rect = UiFactory.Stretch(canvas, "Safe area");
            rect.gameObject.AddComponent<SafeAreaPanel>().ApplyIfChanged(Screen.safeArea, ScreenSize);   // Awake doesn't run in EditMode
            return rect;
        }

        static Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);

        void Update() => ApplyIfChanged(Screen.safeArea, ScreenSize);

        /// <summary>Anchors the panel to this safe area, unless it is the one already applied. Update passes the live values.</summary>
        public void ApplyIfChanged(Rect safeArea, Vector2 screenSize)
        {
            if (_applied && safeArea == _appliedArea && screenSize == _appliedScreen) return;
            _applied = true;
            _appliedArea = safeArea;
            _appliedScreen = screenSize;
            SafeArea.Anchors(safeArea, screenSize, out var min, out var max);
            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
        }
    }
}
