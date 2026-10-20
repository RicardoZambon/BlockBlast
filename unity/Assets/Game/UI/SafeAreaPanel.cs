#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>A stretched UI panel that covers only the safe area, and follows it when it changes (rotation, a fold).</summary>
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        Rect _appliedArea;
        Vector2 _appliedScreen;

        public static RectTransform Create(Transform canvas)
        {
            var rect = UiFactory.Stretch(canvas, "Safe area");
            rect.gameObject.AddComponent<SafeAreaPanel>().Apply();
            return rect;
        }

        void Update()
        {
            if (Screen.safeArea != _appliedArea || new Vector2(Screen.width, Screen.height) != _appliedScreen) Apply();
        }

        void Apply()
        {
            _appliedArea = Screen.safeArea;
            _appliedScreen = new Vector2(Screen.width, Screen.height);
            SafeArea.Anchors(_appliedArea, _appliedScreen, out var min, out var max);
            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
        }
    }
}
