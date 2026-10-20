#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The game-over panel: the final score, the best, a slot for the revive offer (Phase 3), then Play again (Classic)
    /// or Share (the Daily), and Home.
    /// </summary>
    public sealed class GameOverView : MonoBehaviour
    {
        Text? _scores;
        Action? _playAgain;
        Action? _share;
        Action? _home;

        /// <summary>Classic's panel: Play again and Home.</summary>
        public static GameOverView Create(Transform canvas, Action playAgain, Action home) => Create(canvas, "Game over", playAgain, null, home);

        public static GameOverView Create(Transform canvas, string title, Action? playAgain, Action? share, Action home)
        {
            var root = UiFactory.Dim(canvas, "Game over");   // drags are already refused here: the game is over
            var view = root.gameObject.AddComponent<GameOverView>();
            view._playAgain = playAgain;
            view._share = share;
            view._home = home;
            var centre = new Vector2(0.5f, 0.5f);
            UiFactory.Text(UiFactory.Rect(root, "Title", centre, new Vector2(900, 160), new Vector2(0, 460)), 110, Color.white).text = title;
            view._scores = UiFactory.Text(UiFactory.Rect(root, "Scores", centre, new Vector2(900, 220), new Vector2(0, 240)), 64, Color.white);

            // Reserved now, so adding the rewarded revive in Phase 3 doesn't move the buttons.
            view.ReviveSlot = UiFactory.Rect(root, "Revive slot", centre, new Vector2(700, 180), new Vector2(0, 30));
            view.ReviveSlot.gameObject.SetActive(false);

            float y = -180;
            var size = new Vector2(600, 170);
            if (playAgain != null)
            {
                UiFactory.Button(root, "Play again", "Play again", centre, size, new Vector2(0, y), view.PressPlayAgain);
                y -= 200;
            }
            if (share != null)
            {
                UiFactory.Button(root, "Share", "Share", centre, size, new Vector2(0, y), view.PressShare);
                y -= 200;
            }
            UiFactory.Button(root, "Home", "Home", centre, size, new Vector2(0, y), view.PressHome);
            return view;
        }

        /// <summary>Where the revive offer goes; inactive until Phase 3 adds rewarded ads.</summary>
        public RectTransform ReviveSlot { get; private set; } = null!;

        public bool IsShown => gameObject.activeSelf;

        public bool OffersPlayAgain => _playAgain != null;

        public bool OffersShare => _share != null;

        public string ScoresText => _scores != null ? _scores.text : "";

        /// <summary>Shows the panel. A streak above 0 adds a line for it.</summary>
        public void Show(int score, int best, int streak = 0)
        {
            if (_scores != null) _scores.text = "Score " + score + "\nBest " + best + (streak > 0 ? "\n" + streak + "-day streak" : "");
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>What the Play again button does; tests call it directly.</summary>
        public void PressPlayAgain() => _playAgain?.Invoke();

        /// <summary>What the Share button does; tests call it directly.</summary>
        public void PressShare() => _share?.Invoke();

        /// <summary>What the Home button does; tests call it directly.</summary>
        public void PressHome() => _home?.Invoke();
    }
}
