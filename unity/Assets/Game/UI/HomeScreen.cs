#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>Home: Play, or Continue when a game is in progress; the Daily Puzzle, disabled until task 2.5; and Settings.</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        Text? _playLabel;
        Button? _daily;
        Action? _play;
        Action? _settings;

        public static HomeScreen Create(Transform parent, Action play, Action settings)
        {
            var canvas = UiFactory.Canvas(parent, "Home");
            var screen = canvas.gameObject.AddComponent<HomeScreen>();
            screen._play = play;
            screen._settings = settings;
            var safe = SafeAreaPanel.Create(canvas);
            var centre = new Vector2(0.5f, 0.5f);

            UiFactory.Text(UiFactory.Rect(safe, "Title", centre, new Vector2(1000, 200), new Vector2(0, 520)), 120, Color.white).text = Application.productName;
            screen._playLabel = UiFactory.Button(safe, "Play", "Play", centre, new Vector2(700, 200), new Vector2(0, 120), screen.PressPlay)
                .GetComponentInChildren<Text>();
            screen._daily = UiFactory.Button(safe, "Daily", "Daily · soon", centre, new Vector2(700, 170), new Vector2(0, -140), () => { });
            screen._daily.interactable = false;
            UiFactory.Button(safe, "Settings", "Settings", centre, new Vector2(700, 170), new Vector2(0, -360), screen.PressSettings);
            return screen;
        }

        public string PlayLabel => _playLabel != null ? _playLabel.text : "";

        public string DailyLabel => _daily != null ? _daily.GetComponentInChildren<Text>().text : "";

        public bool DailyEnabled => _daily != null && _daily.interactable;

        /// <summary>"Continue" while a game is in progress, and "Play" otherwise, including right after a game over.</summary>
        public void Refresh(bool gameInProgress)
        {
            if (_playLabel != null) _playLabel.text = gameInProgress ? "Continue" : "Play";
        }

        /// <summary>What the Play button does; tests call it directly.</summary>
        public void PressPlay() => _play?.Invoke();

        /// <summary>What the Settings button does; tests call it directly.</summary>
        public void PressSettings() => _settings?.Invoke();
    }
}
