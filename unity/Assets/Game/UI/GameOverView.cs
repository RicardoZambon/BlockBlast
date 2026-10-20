#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>The game-over panel: the final score, the best, a slot for the revive offer (Phase 3), Play again and Home.</summary>
    public sealed class GameOverView : MonoBehaviour
    {
        Text? _scores;
        Action? _playAgain;
        Action? _home;

        public static GameOverView Create(Transform canvas, Action playAgain, Action home)
        {
            var root = UiFactory.Dim(canvas, "Game over");   // drags are already refused here: the game is over
            var view = root.gameObject.AddComponent<GameOverView>();
            view._playAgain = playAgain;
            view._home = home;
            var centre = new Vector2(0.5f, 0.5f);
            UiFactory.Text(UiFactory.Rect(root, "Title", centre, new Vector2(900, 160), new Vector2(0, 460)), 110, Color.white).text = "Game over";
            view._scores = UiFactory.Text(UiFactory.Rect(root, "Scores", centre, new Vector2(900, 220), new Vector2(0, 240)), 64, Color.white);

            // Reserved now, so adding the rewarded revive in Phase 3 doesn't move the buttons.
            view.ReviveSlot = UiFactory.Rect(root, "Revive slot", centre, new Vector2(700, 180), new Vector2(0, 30));
            view.ReviveSlot.gameObject.SetActive(false);

            UiFactory.Button(root, "Play again", "Play again", centre, new Vector2(600, 170), new Vector2(0, -180), view.PressPlayAgain);
            UiFactory.Button(root, "Home", "Home", centre, new Vector2(600, 170), new Vector2(0, -380), view.PressHome);
            return view;
        }

        /// <summary>Where the revive offer goes; inactive until Phase 3 adds rewarded ads.</summary>
        public RectTransform ReviveSlot { get; private set; } = null!;

        public bool IsShown => gameObject.activeSelf;

        public void Show(int score, int best)
        {
            if (_scores != null) _scores.text = "Score " + score + "\nBest " + best;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>What the Play again button does; tests call it directly.</summary>
        public void PressPlayAgain() => _playAgain?.Invoke();

        /// <summary>What the Home button does; tests call it directly.</summary>
        public void PressHome() => _home?.Invoke();
    }
}
