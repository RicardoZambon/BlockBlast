#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>The tutorial's one line of text, in the band at the top of the safe area, and a Skip button.</summary>
    public sealed class TutorialHintView : MonoBehaviour
    {
        /// <summary>Clears Skip (30 to 230 from the left edge) and, mirrored, the development Bench button on the right.</summary>
        const float SideMargin = 260f;

        Text? _text;
        Action? _skip;

        public static TutorialHintView Create(Transform safeArea, Action skip)
        {
            var root = UiFactory.Stretch(safeArea, "Tutorial hint");
            var view = root.gameObject.AddComponent<TutorialHintView>();
            view._skip = skip;
            // As wide as the screen allows between the margins: tall phones have a narrower canvas.
            var hint = UiFactory.Rect(root, "Hint", new Vector2(0.5f, 1f), new Vector2(0, 120), new Vector2(0, -70));
            hint.anchorMin = new Vector2(0f, 1f);
            hint.anchorMax = new Vector2(1f, 1f);
            hint.sizeDelta = new Vector2(-2 * SideMargin, 120);
            view._text = UiFactory.FittedText(hint, 56, Color.white);
            UiFactory.Button(root, "Skip", Strings.Get("tutorial.skip"), new Vector2(0f, 1f), new Vector2(200, 100), new Vector2(130, -65), view.PressSkip);
            return view;
        }

        public string Text => _text != null ? _text.text : "";

        public void Show(string hint)
        {
            if (_text != null) _text.text = hint;
        }

        /// <summary>What the Skip button does; tests call it directly.</summary>
        public void PressSkip() => _skip?.Invoke();
    }
}
