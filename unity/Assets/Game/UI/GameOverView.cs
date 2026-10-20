#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>The game-over panel: the final score, the best, and Play again.</summary>
    public sealed class GameOverView : MonoBehaviour
    {
        Text? _scores;
        Action? _playAgain;

        public static GameOverView Create(Transform canvas, Action playAgain)
        {
            var root = UiFactory.Stretch(canvas, "Game over");
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);   // drags are already refused here: the game is over

            var view = root.gameObject.AddComponent<GameOverView>();
            view._playAgain = playAgain;
            var centre = new Vector2(0.5f, 0.5f);
            UiFactory.Text(UiFactory.Rect(root, "Title", centre, new Vector2(900, 160), new Vector2(0, 360)), 110, Color.white).text = "Game over";
            view._scores = UiFactory.Text(UiFactory.Rect(root, "Scores", centre, new Vector2(900, 220), new Vector2(0, 120)), 64, Color.white);

            var buttonRect = UiFactory.Rect(root, "Play again", centre, new Vector2(600, 170), new Vector2(0, -200));
            buttonRect.gameObject.AddComponent<Image>().color = Palette.Filled;
            buttonRect.gameObject.AddComponent<Button>().onClick.AddListener(view.PressPlayAgain);
            UiFactory.Text(UiFactory.Stretch(buttonRect, "Label"), 64, Color.white).text = "Play again";
            return view;
        }

        public bool IsShown => gameObject.activeSelf;

        public void Show(int score, int best)
        {
            if (_scores != null) _scores.text = "Score " + score + "\nBest " + best;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>What the Play again button does; tests call it directly.</summary>
        public void PressPlayAgain() => _playAgain?.Invoke();
    }
}
