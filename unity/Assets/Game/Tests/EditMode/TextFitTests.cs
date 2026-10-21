#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>
    /// Every text on every screen shows whole in its box, in every language, on the narrowest screen (9:21) and with the
    /// longest values it can show: a best-fit text at its smallest size, any other at its own size.
    /// </summary>
    public class TextFitTests
    {
        const int Width = 1080, Height = 2520;   // 9:21

        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<string> _overflows = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) Object.DestroyImmediate(go);
            _created.Clear();
            _overflows.Clear();
            Strings.Current = Language.English;
        }

        /// <summary>The canvas size UiFactory's CanvasScaler gives the screen: 1080 × 1920 reference, matched halfway.</summary>
        static Vector2 CanvasSize()
        {
            float scale = Mathf.Sqrt(Width / 1080f * (Height / 1920f));
            return new Vector2(Width / scale, Height / scale);
        }

        Transform Parent()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            _created.Add(go);
            go.AddComponent<Canvas>();
            ((RectTransform)go.transform).sizeDelta = CanvasSize();
            return go.transform;
        }

        /// <summary>A screen with its own canvas: its safe-area panel follows Screen only in Play mode, so size it here.</summary>
        static void Size(Component screen)
        {
            var safe = (RectTransform)screen.transform.Find("Safe area");
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = CanvasSize();
        }

        void Check(string state, Component root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>())
            {
                string? problem = Overflow(text);
                if (problem != null) _overflows.Add($"{Strings.Current}, {state}: \"{text.text.Replace("\n", " / ")}\" ({text.transform.parent.name}/{text.name}) {problem}");
            }
        }

        static string? Overflow(Text text)
        {
            if (string.IsNullOrEmpty(text.text)) return null;
            var box = text.rectTransform.rect.size;
            var generator = new TextGenerator();
            if (text.resizeTextForBestFit)
            {
                var settings = text.GetGenerationSettings(new Vector2(box.x, 0f));
                settings.resizeTextForBestFit = false;
                settings.fontSize = text.resizeTextMinSize;
                settings.verticalOverflow = VerticalWrapMode.Overflow;
                float height = generator.GetPreferredHeight(text.text, settings) / settings.scaleFactor;
                return height <= box.y + 0.5f ? null : $"needs {height:F0} of {box.y:F0} high at size {text.resizeTextMinSize}";
            }
            var own = text.GetGenerationSettings(new Vector2(text.horizontalOverflow == HorizontalWrapMode.Wrap ? box.x : 0f, 0f));
            float width = generator.GetPreferredWidth(text.text, own) / own.scaleFactor;
            float tall = generator.GetPreferredHeight(text.text, own) / own.scaleFactor;
            if (width > box.x + 0.5f) return $"needs {width:F0} of {box.x:F0} wide";
            if (tall > box.y + 0.5f) return $"needs {tall:F0} of {box.y:F0} high";
            return null;
        }

        [Test]
        public void Every_text_fits_in_every_language([Values] Language language)
        {
            Strings.Current = language;

            var home = HomeScreen.Create(Parent(), () => { }, () => { }, () => { });
            Size(home);
            home.Refresh(gameInProgress: false, DailyState.Start, streak: 365);
            Check("Home, a long streak", home);
            home.Refresh(gameInProgress: true, DailyState.Continue, streak: 0);
            Check("Home, Continue and the Daily under way", home);
            home.Refresh(gameInProgress: false, DailyState.Done, streak: 0);
            Check("Home, the Daily done", home);

            var settings = SettingsScreen.Create(Parent(), new SettingsData(), () => { }, () => { }, () => { }, () => { }, () => { });
            Size(settings);
            settings.ShowPrivacySettings(true);
            settings.ShowRemoveAds(owned: false, price: "R$ 14,90");
            Check("Settings, with a price", settings);
            settings.ShowRemoveAds(owned: true, price: null);
            Check("Settings, ads removed", settings);

            Check("Pause", PauseView.Create(Parent(), () => { }, () => { }));

            var classic = GameOverView.Create(Parent(), Strings.Get("game_over.title"), () => { }, null, () => { }, () => { });
            classic.ShowRevive(true);
            classic.Show(score: 9_999_999, best: 9_999_999);
            Check("Game over", classic);
            var daily = GameOverView.Create(Parent(), Strings.Get("game_over.daily_title"), null, () => { }, () => { });
            daily.Show(score: 9_999_999, best: 9_999_999, streak: 365);
            Check("Daily done", daily);

            var hud = HudView.Create(Parent(), 0.8f, 0.9f, () => { });
            hud.Show(score: 9_999_999, best: 9_999_999);
            hud.ShowStreak(99);
            Check("HUD", hud);

            var hint = TutorialHintView.Create(Parent(), () => { });
            foreach (var key in new[] { "tutorial.drag", "tutorial.row", "tutorial.column" })
            {
                hint.Show(Strings.Get(key));
                Check("Tutorial", hint);
            }

            Assert.IsEmpty(_overflows, string.Join("\n", _overflows));
        }
    }
}
