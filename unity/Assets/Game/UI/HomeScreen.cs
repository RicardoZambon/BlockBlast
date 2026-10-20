#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>Home: Play, or Continue when a Classic game is in progress; the Daily Puzzle; and Settings.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        Text? _playLabel;
        Text? _dailyLabel;
        Button? _daily;
        Action? _play;
        Action? _playDaily;
        Action? _settings;

        public static HomeScreen Create(Transform parent, Action play, Action daily, Action settings)
        {
            var canvas = UiFactory.Canvas(parent, "Home");
            var screen = canvas.gameObject.AddComponent<HomeScreen>();
            screen._play = play;
            screen._playDaily = daily;
            screen._settings = settings;
            var safe = SafeAreaPanel.Create(canvas);
            var centre = new Vector2(0.5f, 0.5f);

            UiFactory.Text(UiFactory.Rect(safe, "Title", centre, new Vector2(1000, 200), new Vector2(0, 520)), 120, Color.white).text = Application.productName;
            screen._playLabel = UiFactory.Button(safe, "Play", "Play", centre, new Vector2(700, 200), new Vector2(0, 120), screen.PressPlay)
                .GetComponentInChildren<Text>();
            screen._daily = UiFactory.Button(safe, "Daily", "Daily", centre, new Vector2(700, 170), new Vector2(0, -140), screen.PressDaily);
            screen._dailyLabel = screen._daily.GetComponentInChildren<Text>();
            UiFactory.Button(safe, "Settings", "Settings", centre, new Vector2(700, 170), new Vector2(0, -360), screen.PressSettings);
            return screen;
        }

        public string PlayLabel => _playLabel != null ? _playLabel.text : "";

        public string DailyLabel => _dailyLabel != null ? _dailyLabel.text : "";

        public bool DailyEnabled => _daily != null && _daily.interactable;

        /// <summary>
        /// Play reads Continue while a Classic game is in progress, and Play otherwise, including right after a game over.
        /// Daily reads Daily (with the streak), continue, or done, and is disabled when done.
        /// </summary>
        public void Refresh(bool gameInProgress, DailyState daily, int streak)
        {
            if (_playLabel != null) _playLabel.text = gameInProgress ? "Continue" : "Play";
            if (_dailyLabel != null)
            {
                _dailyLabel.text = daily switch
                {
                    DailyState.Continue => "Daily · continue",
                    DailyState.Done => "Daily · done",
                    _ => streak > 0 ? "Daily · " + streak + "-day streak" : "Daily",
                };
            }
            if (_daily != null) _daily.interactable = daily != DailyState.Done;
        }

        /// <summary>What the Play button does; tests call it directly.</summary>
        public void PressPlay() => _play?.Invoke();

        /// <summary>What the Daily button does; like the button, nothing while it is disabled.</summary>
        public void PressDaily()
        {
            if (DailyEnabled) _playDaily?.Invoke();
        }

        /// <summary>What the Settings button does; tests call it directly.</summary>
        public void PressSettings() => _settings?.Invoke();
    }
}
