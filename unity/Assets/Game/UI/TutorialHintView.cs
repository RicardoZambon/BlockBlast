#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>The tutorial's one line of text, in the band at the top of the safe area, and a Skip button.</summary>
    public sealed class TutorialHintView : MonoBehaviour
    {
        Text? _text;
        Action? _skip;

        public static TutorialHintView Create(Transform safeArea, Action skip)
        {
            var root = UiFactory.Stretch(safeArea, "Tutorial hint");
            var view = root.gameObject.AddComponent<TutorialHintView>();
            view._skip = skip;
            // Between Skip on the left and the development Bench button on the right.
            view._text = UiFactory.FittedText(UiFactory.Rect(root, "Hint", new Vector2(0.5f, 1f), new Vector2(620, 120), new Vector2(0, -70)), 56, Color.white);
            UiFactory.Button(root, "Skip", "Skip", new Vector2(0f, 1f), new Vector2(200, 100), new Vector2(130, -65), view.PressSkip);
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
