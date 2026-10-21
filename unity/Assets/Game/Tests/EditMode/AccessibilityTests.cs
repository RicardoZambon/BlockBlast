#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>
    /// Master plan 2.9, as far as there is UI to check: readable contrast (WCAG 2.1 ratios), board states told apart by
    /// brightness and not only by colour, and a minimum text size on every screen.
    /// </summary>
    public class AccessibilityTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (var go in _created) UnityEngine.Object.DestroyImmediate(go);
            _created.Clear();
        }

        Transform Canvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            _created.Add(go);
            go.AddComponent<Canvas>();
            return go.transform;
        }

        static double Luminance(Color c)
        {
            static double Linear(float v) => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            return 0.2126 * Linear(c.r) + 0.7152 * Linear(c.g) + 0.0722 * Linear(c.b);
        }

        static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        /// <summary>A see-through colour as it shows over another.</summary>
        static Color Over(Color top, Color bottom) => new Color(
            top.r * top.a + bottom.r * (1 - top.a), top.g * top.a + bottom.g * (1 - top.a), top.b * top.a + bottom.b * (1 - top.a), 1f);

        [Test]
        public void Text_on_the_background_meets_4_5_to_1()
        {
            Assert.GreaterOrEqual(Contrast(Color.white, Palette.Background), 4.5);
            Assert.GreaterOrEqual(Contrast(Over(new Color(1f, 1f, 1f, 0.6f), Palette.Background), Palette.Background), 4.5, "the best score and the version");
            Assert.GreaterOrEqual(Contrast(Palette.Clearing, Palette.Background), 4.5, "the streak");
        }

        [Test]
        public void Button_labels_meet_4_5_to_1_and_buttons_stand_out()
        {
            Assert.GreaterOrEqual(Contrast(Color.white, Palette.Button), 4.5);
            Assert.GreaterOrEqual(Contrast(Palette.Button, Palette.Background), 3.0, "a button against the background");
        }

        [Test]
        public void Board_states_differ_by_brightness_not_only_colour()
        {
            var states = new[]
            {
                ("empty", Palette.Empty),
                ("filled", Palette.Filled),
                ("ghost", Over(Palette.Ghost, Palette.Empty)),
                ("clearing", Palette.Clearing),
            };

            for (int i = 0; i < states.Length; i++)
            {
                for (int j = i + 1; j < states.Length; j++)
                {
                    Assert.GreaterOrEqual(Contrast(states[i].Item2, states[j].Item2), 1.5, states[i].Item1 + " against " + states[j].Item1);
                }
            }
            Assert.GreaterOrEqual(Contrast(Palette.Filled, Palette.Empty), 3.0, "a filled cell against an empty one");
        }

        [Test]
        public void Every_screen_keeps_its_text_at_or_above_the_minimum_size()
        {
            var roots = new List<Component>
            {
                HomeScreen.Create(Canvas(), () => { }, () => { }, () => { }),
                SettingsScreen.Create(Canvas(), new SettingsData(), () => { }, () => { }),
                HudView.Create(Canvas(), 0.8f, 0.9f, () => { }),
                PauseView.Create(Canvas(), () => { }, () => { }),
                GameOverView.Create(Canvas(), () => { }, () => { }),
                GameOverView.Create(Canvas(), "Daily done", null, () => { }, () => { }),
                TutorialHintView.Create(Canvas(), () => { }),
            };

            var texts = roots.SelectMany(root => root.GetComponentsInChildren<Text>(includeInactive: true)).ToList();

            Assert.Greater(texts.Count, 20);
            foreach (var text in texts)
            {
                int smallest = text.resizeTextForBestFit ? text.resizeTextMinSize : text.fontSize;
                Assert.GreaterOrEqual(smallest, Readability.MinTextSize, text.name + " under " + text.transform.parent.name);
            }
        }
    }
}
