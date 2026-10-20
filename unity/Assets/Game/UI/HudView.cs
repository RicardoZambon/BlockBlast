#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>The score and best score at the top of the screen.</summary>
    public sealed class HudView : MonoBehaviour
    {
        Text? _score;
        Text? _best;
        int _shownScore = -1;
        int _shownBest = -1;

        public static HudView Create(Transform canvas)
        {
            var root = UiFactory.Stretch(canvas, "Hud");
            var view = root.gameObject.AddComponent<HudView>();
            var top = new Vector2(0.5f, 1f);
            view._score = UiFactory.Text(UiFactory.Rect(root, "Score", top, new Vector2(900, 140), new Vector2(0, -150)), 110, Color.white);
            view._best = UiFactory.Text(UiFactory.Rect(root, "Best", top, new Vector2(900, 60), new Vector2(0, -60)), 44, new Color(1f, 1f, 1f, 0.6f));
            view.Show(0, 0);
            return view;
        }

        public string ScoreText => _score != null ? _score.text : "";

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
    }
}
