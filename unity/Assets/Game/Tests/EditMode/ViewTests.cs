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
        public void A_safe_area_panel_matches_the_safe_area_anchors()
        {
            var panel = SafeAreaPanel.Create(Canvas());

            SafeArea.Anchors(Screen.safeArea, new Vector2(Screen.width, Screen.height), out var min, out var max);
            Assert.AreEqual(min, panel.anchorMin);
            Assert.AreEqual(max, panel.anchorMax);
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
