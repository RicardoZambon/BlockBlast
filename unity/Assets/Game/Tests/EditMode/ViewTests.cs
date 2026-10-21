#nullable enable
using System.Collections.Generic;
using System.Linq;
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace BlockPuzzle.Game.Tests
{
    public class ViewTests
    {
        static readonly BoardLayout Layout = new BoardLayout(new Rect(-4.5f, -8f, 9f, 16f));
        readonly List<GameObject> _created = new List<GameObject>();

        T Make<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            _created.Add(go);
            return go.AddComponent<T>();
        }

        [TearDown]
        public void DestroyCreated()
        {
            foreach (var go in _created) Object.DestroyImmediate(go);
            _created.Clear();
        }

        [Test]
        public void Render_colours_filled_and_empty_cells()
        {
            var board = Make<BoardView>();
            board.Build(Layout);

            board.Render(TestBoards.AlmostFullRow);

            Assert.AreEqual(Palette.Filled, board.CellColor(0, 0));
            Assert.AreEqual(Palette.Empty, board.CellColor(0, 6));
        }

        [Test]
        public void ShowGhost_marks_ghost_cells_and_the_lines_that_would_clear()
        {
            var board = Make<BoardView>();
            board.Build(Layout);

            board.ShowGhost(TestBoards.AlmostFullRow, TestBoards.Cell(0, 6) | TestBoards.Cell(0, 7), Board.RowMask(0));
            for (int c = 0; c < 8; c++) Assert.AreEqual(Palette.Clearing, board.CellColor(0, c));
            Assert.AreEqual(Palette.Empty, board.CellColor(1, 0));

            board.ShowGhost(TestBoards.AlmostFullRow, TestBoards.Cell(3, 3), 0);   // a ghost that clears nothing
            Assert.AreEqual(Palette.Ghost, board.CellColor(3, 3));
            Assert.AreEqual(Palette.Filled, board.CellColor(0, 0));
            Assert.AreEqual(Palette.Empty, board.CellColor(0, 6));

            board.Render(TestBoards.AlmostFullRow);
            Assert.AreEqual(Palette.Empty, board.CellColor(3, 3));
        }

        [Test]
        public void ShowGhost_allocates_nothing()
        {
            var board = Make<BoardView>();
            board.Build(Layout);
            board.ShowGhost(TestBoards.AlmostFullRow, TestBoards.Cell(0, 6), 0);

            Assert.That(() =>
            {
                board.ShowGhost(TestBoards.AlmostFullRow, TestBoards.Cell(0, 6) | TestBoards.Cell(0, 7), Board.RowMask(0));
                board.ShowGhost(TestBoards.AlmostFullRow, TestBoards.Cell(4, 4), 0);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void A_piece_view_shows_one_square_per_cell()
        {
            var piece = Make<PieceView>();

            piece.Show(PieceCatalog.Standard["o3"], 1f);
            Assert.AreEqual(9, piece.VisibleCells);

            piece.Show(PieceCatalog.Standard["l4a"], 2f);   // "#.", "#.", "##"
            Assert.AreEqual(4, piece.VisibleCells);
            Assert.AreEqual("l4a", piece.Shape!.Id);
            var expected = new[] { (0, 0), (1, 0), (2, 0), (2, 1) }
                .Select(cell => new Vector2((cell.Item2 + 0.5f) * 2f, -(cell.Item1 + 0.5f) * 2f))
                .OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
            var actual = Enumerable.Range(0, piece.transform.childCount)
                .Select(i => piece.transform.GetChild(i))
                .Where(child => child.gameObject.activeSelf)
                .Select(child => (Vector2)child.localPosition)
                .OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        Transform Canvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            _created.Add(go);
            go.AddComponent<Canvas>();
            return go.transform;
        }

        /// <summary>The canvas size UiFactory's CanvasScaler gives a screen: 1080 × 1920 reference, matched halfway.</summary>
        static Vector2 CanvasSize(int width, int height)
        {
            float scale = Mathf.Sqrt(width / 1080f * (height / 1920f));
            return new Vector2(width / scale, height / scale);
        }

        RectTransform SafeArea(int width, int height)
        {
            var go = new GameObject("Safe area", typeof(RectTransform));
            _created.Add(go);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = CanvasSize(width, height);
            return rect;
        }

        /// <summary>Settings as a screen of this size shows it, with every row it can have.</summary>
        RectTransform SettingsSafeArea(int width, int height)
        {
            var settings = SettingsScreen.Create(Canvas(), new SettingsData(), () => { }, () => { }, () => { }, () => { }, () => { });
            settings.ShowPrivacySettings(true);
            // The safe-area panel follows Screen only in Play mode, so size it for this screen.
            var safe = (RectTransform)settings.transform.Find("Safe area");
            safe.anchorMin = safe.anchorMax = new Vector2(0.5f, 0.5f);
            safe.sizeDelta = CanvasSize(width, height);
            return safe;
        }

        [TestCase(1080, 1920)]   // 9:16
        [TestCase(1080, 2520)]   // 9:21
        [TestCase(1536, 2048)]   // 3:4, an iPad: the shortest canvas
        public void Settings_rows_stay_on_screen_and_never_overlap(int width, int height)
        {
            var safe = SettingsSafeArea(width, height);
            var screen = Bounds(safe);
            var rows = new[] { "Title", "sound", "music", "haptics", "reduced_motion", "Remove ads", "Restore", "Privacy", "Version", "Back" }
                .Select(name => (name, rect: Bounds(safe.Find(name)))).ToList();

            foreach (var (name, rect) in rows)
            {
                Assert.GreaterOrEqual(rect.yMin, screen.yMin - 0.01f, name + " runs off the bottom");
                Assert.LessOrEqual(rect.yMax, screen.yMax + 0.01f, name + " runs off the top");
            }
            for (int i = 0; i < rows.Count; i++)
                for (int j = i + 1; j < rows.Count; j++)
                    Assert.IsFalse(rows[i].rect.Overlaps(rows[j].rect), $"{rows[i].name} overlaps {rows[j].name}");
        }

        static Rect Bounds(Transform transform)
        {
            var corners = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [TestCase(1080, 1920)]   // 9:16
        [TestCase(1080, 2400)]   // 9:20, a Pixel 6a
        [TestCase(1080, 2520)]   // 9:21
        [TestCase(1536, 2048)]   // 3:4
        public void The_tutorial_hint_keeps_clear_of_Skip_and_the_Bench_corner(int width, int height)
        {
            var safeArea = SafeArea(width, height);
            var view = TutorialHintView.Create(safeArea, () => { });
            // Where the development overlay puts its Bench button.
            var bench = (RectTransform)new GameObject("Bench", typeof(RectTransform)).transform;
            bench.SetParent(safeArea, false);
            bench.anchorMin = bench.anchorMax = new Vector2(1f, 1f);
            bench.sizeDelta = new Vector2(220, 100);
            bench.anchoredPosition = new Vector2(-140, -70);

            var hint = Bounds(view.transform.Find("Hint"));
            Assert.IsFalse(hint.Overlaps(Bounds(view.transform.Find("Skip"))), "the hint runs under Skip");
            Assert.IsFalse(hint.Overlaps(Bounds(bench)), "the hint runs under Bench");
            Assert.Greater(hint.width, 400f, "room for a line of text");
        }

        [Test]
        public void Every_tutorial_hint_fits_the_narrowest_screen_in_every_language()
        {
            var view = TutorialHintView.Create(SafeArea(1080, 2520), () => { });   // 9:21: the narrowest canvas
            var text = view.transform.Find("Hint").GetComponent<UnityEngine.UI.Text>();
            var box = text.rectTransform.rect.size;
            // Fitting at the smallest readable size means best fit never has to cut a line off.
            var settings = text.GetGenerationSettings(new Vector2(box.x, 0f));
            settings.resizeTextForBestFit = false;
            settings.fontSize = Readability.MinTextSize;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var generator = new TextGenerator();
            try
            {
                foreach (Language language in System.Enum.GetValues(typeof(Language)))
                {
                    Strings.Current = language;
                    foreach (var key in new[] { "tutorial.drag", "tutorial.row", "tutorial.column" })
                    {
                        float height = generator.GetPreferredHeight(Strings.Get(key), settings) / settings.scaleFactor;
                        Assert.LessOrEqual(height, box.y, $"{language} {key}");
                    }
                }
            }
            finally
            {
                Strings.Current = Language.English;
            }
        }

        [Test]
        public void The_pause_overlay_calls_resume_and_home()
        {
            int resumed = 0, home = 0;
            var pause = PauseView.Create(Canvas(), () => resumed++, () => home++);
            Assert.IsFalse(pause.IsShown);

            pause.Show();
            Assert.IsTrue(pause.IsShown);
            pause.PressResume();
            pause.PressHome();

            Assert.AreEqual(1, resumed);
            Assert.AreEqual(1, home);
        }

        [Test]
        public void Game_over_has_home_and_a_hidden_revive_slot()
        {
            int home = 0;
            var gameOver = GameOverView.Create(Canvas(), () => { }, () => home++);

            gameOver.PressHome();

            Assert.AreEqual(1, home);
            Assert.IsFalse(gameOver.ReviveSlot.gameObject.activeSelf);
        }

        [Test]
        public void A_daily_game_over_offers_share_and_no_play_again()
        {
            int shared = 0;
            var gameOver = GameOverView.Create(Canvas(), "Daily done", playAgain: null, share: () => shared++, home: () => { });

            gameOver.Show(1240, 1500, streak: 3);
            gameOver.PressShare();

            Assert.AreEqual(1, shared);
            Assert.IsFalse(gameOver.OffersPlayAgain);
            Assert.IsTrue(gameOver.OffersShare);
            StringAssert.Contains("3-day streak", gameOver.ScoresText);
        }

        [Test]
        public void The_hud_pause_button_calls_pause_and_the_score_shows()
        {
            int paused = 0;
            var hud = HudView.Create(Canvas(), 0.8f, 0.9f, () => paused++);

            hud.PressPause();
            hud.Show(120, 300);

            Assert.AreEqual(1, paused);
            Assert.AreEqual("120", hud.ScoreText);
            var rect = (RectTransform)hud.transform;
            Assert.AreEqual(0.8f, rect.anchorMin.y, 1e-5f);
            Assert.AreEqual(0.9f, rect.anchorMax.y, 1e-5f);
        }

        [Test]
        public void A_safe_area_panel_follows_a_new_safe_area()
        {
            var panel = SafeAreaPanel.Create(Canvas());

            // In the Editor the safe area is the whole screen; on a phone a notch and a gesture bar take their share.
            panel.GetComponent<SafeAreaPanel>().ApplyIfChanged(new Rect(0, 96, 1080, 2216), new Vector2(1080, 2400));

            Assert.AreEqual(0f, panel.anchorMin.x, 1e-5f);
            Assert.AreEqual(0.04f, panel.anchorMin.y, 1e-5f);
            Assert.AreEqual(1f, panel.anchorMax.x, 1e-5f);
            Assert.AreEqual(0.963333f, panel.anchorMax.y, 1e-5f);
        }

        static (bool sound, bool music, bool haptics, bool reducedMotion) Values(SettingsData s) =>
            (s.Sound, s.Music, s.Haptics, s.ReducedMotion);

        [Test]
        public void Each_settings_toggle_changes_only_its_own_setting_and_reports_it()
        {
            var settings = new SettingsData();
            int changes = 0;
            var screen = SettingsScreen.Create(Canvas(), settings, () => changes++, () => { });
            Assert.AreEqual(0, changes, "building the screen changes nothing");

            screen.SetToggle("sound", false);
            Assert.AreEqual((false, true, true, false), Values(settings));
            screen.SetToggle("music", false);
            Assert.AreEqual((false, false, true, false), Values(settings));
            screen.SetToggle("haptics", false);
            Assert.AreEqual((false, false, false, false), Values(settings));
            screen.SetToggle("reduced_motion", true);
            Assert.AreEqual((false, false, false, true), Values(settings));
            Assert.AreEqual(4, changes);
            Assert.IsTrue(screen.IsOn("reduced_motion"));
        }

        [Test]
        public void The_tray_shows_each_piece_and_hides_empty_slots()
        {
            var tray = Make<TrayView>();
            tray.Build(Layout);

            tray.Render(TestGames.Position(Board.Empty, "i2h", "", "o3"));

            Assert.IsTrue(tray.Slot(0).gameObject.activeSelf);
            Assert.AreEqual("i2h", tray.Slot(0).Shape!.Id);
            Assert.IsFalse(tray.Slot(1).gameObject.activeSelf);
            Assert.AreEqual("o3", tray.Slot(2).Shape!.Id);
            Assert.AreEqual(BoardLayout.TrayScale, tray.Slot(2).transform.localScale.x, 1e-5f);
            Vector2 expected = Layout.TrayPieceTopLeft(2, 3, 3);
            Assert.AreEqual(expected.x, tray.Slot(2).transform.position.x, 1e-4f);
            Assert.AreEqual(expected.y, tray.Slot(2).transform.position.y, 1e-4f);
        }
    }
}
