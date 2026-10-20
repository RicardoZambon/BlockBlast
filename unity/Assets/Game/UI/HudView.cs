#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The score, the best score and the pause button, laid exactly over the score band the board layout reserves, which
    /// is already inside the safe area and below the banner slot.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        Text? _score;
        Text? _best;
        Text? _streak;
        Action? _pause;
        int _shownScore = -1;
        int _shownBest = -1;
        int _shownStreak = -1;

        /// <param name="bandBottom">The bottom of the score band, as a fraction of the screen height.</param>
        /// <param name="bandTop">The top of the score band, as a fraction of the screen height.</param>
        public static HudView Create(Transform canvas, float bandBottom, float bandTop, Action pause)
        {
            var root = UiFactory.Stretch(canvas, "Hud");
            root.anchorMin = new Vector2(0f, bandBottom);
            root.anchorMax = new Vector2(1f, bandTop);
            var view = root.gameObject.AddComponent<HudView>();
            view._pause = pause;

            view._score = UiFactory.FittedText(Band(root, "Score", 0.25f, 0.75f, 0.3f, 1f), 110, Color.white);
            view._best = UiFactory.FittedText(Band(root, "Best", 0.25f, 0.75f, 0f, 0.3f), 44, new Color(1f, 1f, 1f, 0.6f));
            view._streak = UiFactory.FittedText(Band(root, "Streak", 0.78f, 0.97f, 0.15f, 0.85f), 48, Palette.Clearing);

            var button = Band(root, "Pause", 0.03f, 0.15f, 0.15f, 0.85f);
            button.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            button.gameObject.AddComponent<Button>().onClick.AddListener(view.PressPause);
            UiFactory.FittedText(UiFactory.Stretch(button, "Label"), 64, Color.white).text = "II";

            view.Show(0, 0);
            view.ShowStreak(0);
            return view;
        }

        public string ScoreText => _score != null ? _score.text : "";

        public string StreakText => _streak != null ? _streak.text : "";

        /// <summary>"Streak N" from a streak of two; nothing below. Changes the text only when the streak changes.</summary>
        public void ShowStreak(int streak)
        {
            if (_streak == null || streak == _shownStreak) return;
            _shownStreak = streak;
            _streak.text = streak >= 2 ? "Streak " + streak : "";
        }

        /// <summary>Changes the texts only when a value changes, so a frame without a new score allocates nothing.</summary>
        public void Show(int score, int best)
        {
            if (_score != null && score != _shownScore)
            {
                _shownScore = score;
                _score.text = score.ToString();
            }
            if (_best != null && best != _shownBest)
            {
                _shownBest = best;
                _best.text = "Best " + best;
            }
        }

        /// <summary>What the pause button does; tests call it directly.</summary>
        public void PressPause() => _pause?.Invoke();

        static RectTransform Band(Transform parent, string name, float xMin, float xMax, float yMin, float yMax)
        {
            var rect = UiFactory.Stretch(parent, name);
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            return rect;
        }
    }
}
