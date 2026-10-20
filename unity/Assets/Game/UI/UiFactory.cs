#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>Builds the prototype's few uGUI elements in code, with Unity's built-in font, so there are no UI assets.</summary>
    static class UiFactory
    {
        static Font? _font;

        static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        /// <summary>A screen-space canvas scaled from a 1080×1920 reference, so sizes read the same on every phone.</summary>
        public static Transform Canvas(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        /// <summary>Creates the EventSystem that buttons need, under parent, unless an active one already exists.</summary>
        public static void EnsureEventSystem(Transform parent)
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Text Text(RectTransform rect, int fontSize, Color color)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Text that shrinks to fit its rectangle, for areas whose size depends on the screen.</summary>
        public static Text FittedText(RectTransform rect, int maxFontSize, Color color)
        {
            var text = Text(rect, maxFontSize, color);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = maxFontSize;
            return text;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Vector2 position, Action onClick)
        {
            var rect = Rect(parent, name, anchor, size, position);
            rect.gameObject.AddComponent<Image>().color = Palette.Filled;
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            Text(Stretch(rect, "Label"), 64, Color.white).text = label;
            return button;
        }

        /// <summary>A stretched, see-through dark layer: the background of an overlay.</summary>
        public static RectTransform Dim(Transform parent, string name)
        {
            var rect = Stretch(parent, name);
            rect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            return rect;
        }
    }
}
