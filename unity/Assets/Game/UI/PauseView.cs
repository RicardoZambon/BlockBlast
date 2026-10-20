#nullable enable
using System;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The pause overlay: Resume, or Home. The game is saved after every move, so leaving loses nothing.</summary>
    public sealed class PauseView : MonoBehaviour
    {
        Action? _resume;
        Action? _home;

        /// <summary>Created hidden.</summary>
        public static PauseView Create(Transform canvas, Action resume, Action home)
        {
            var root = UiFactory.Dim(canvas, "Pause");
            var view = root.gameObject.AddComponent<PauseView>();
            view._resume = resume;
            view._home = home;
            var centre = new Vector2(0.5f, 0.5f);
            UiFactory.Text(UiFactory.Rect(root, "Title", centre, new Vector2(900, 160), new Vector2(0, 300)), 110, Color.white).text = "Paused";
            UiFactory.Button(root, "Resume", "Resume", centre, new Vector2(600, 170), new Vector2(0, 40), view.PressResume);
            UiFactory.Button(root, "Home", "Home", centre, new Vector2(600, 170), new Vector2(0, -180), view.PressHome);
            view.Hide();
            return view;
        }

        public bool IsShown => gameObject.activeSelf;

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        public void PressResume() => _resume?.Invoke();

        public void PressHome() => _home?.Invoke();
    }
}
