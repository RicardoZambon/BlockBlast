#nullable enable
using BlockPuzzle.Core;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace BlockPuzzle.Game.Tests
{
    public class TutorialTests
    {
        [Test]
        public void Each_guided_move_is_legal_and_the_bars_clear_a_line()
        {
            var save = new SaveData();
            var tutorial = new TutorialMode(save);
            var game = tutorial.StartNew();
            int[] lines = { 0, 1, 1 };   // the square anywhere, then a row, then a column

            for (int step = 0; step < lines.Length; step++)
            {
                var guide = tutorial.CurrentGuide!.Value;
                Assert.IsTrue(game.TryPlace(guide.Slot, guide.Row, guide.Col, out var outcome), "step " + step);
                Assert.AreEqual(lines[step], outcome.Lines, "step " + step);
                Assert.IsFalse(tutorial.IsComplete);

                var next = tutorial.NextStep(game);
                if (step == lines.Length - 1)
                {
                    Assert.IsNull(next);
                    break;
                }
                Assert.IsNotNull(next);
                Assert.AreEqual(game.Board, next!.Board, "the next step keeps the board the player made");
                game = next;
            }

            Assert.IsTrue(tutorial.IsComplete);
            Assert.IsNull(tutorial.CurrentGuide);
            Assert.IsTrue(save.TutorialDone);
        }

        [Test]
        public void The_guide_lets_the_piece_land_only_on_its_target()
        {
            var game = TestGames.Position(Board.Empty, "", "o2", "");

            Assert.IsTrue(SnapFinder.TryFind(game, 1, 5.1f, 0.2f, out _, out _), "without a guide, (5, 0) is a fine spot");
            Assert.IsFalse(SnapFinder.TryFind(game, 1, 5.1f, 0.2f, out _, out _, onlyRow: 2, onlyCol: 2));
            Assert.IsTrue(SnapFinder.TryFind(game, 1, 2.3f, 1.8f, out int row, out int col, onlyRow: 2, onlyCol: 2));
            Assert.AreEqual((2, 2), (row, col));

            var session = new DragSession();
            session.SetGuide(2, 2);
            Assert.IsTrue(session.TryBegin(game, 1));
            session.Move(game, 5.1f, 0.2f);
            Assert.IsFalse(session.HasTarget);
            session.Move(game, 2.3f, 1.8f);
            Assert.IsTrue(session.HasTarget);
        }

        [Test]
        public void A_guided_drag_allocates_nothing()
        {
            var game = TestGames.Position(Board.Empty, "", "o2", "");
            var session = new DragSession();
            session.SetGuide(2, 2);
            session.TryBegin(game, 1);
            session.Move(game, 2.3f, 1.8f);

            Assert.That(() =>
            {
                session.Move(game, 2.3f, 1.8f);
                session.Move(game, 5.1f, 0.2f);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Skip_marks_the_tutorial_done()
        {
            var save = new SaveData();
            var tutorial = new TutorialMode(save);
            tutorial.StartNew();

            tutorial.Skip();

            Assert.IsTrue(save.TutorialDone);
            Assert.IsTrue(tutorial.IsComplete);
            Assert.IsNull(tutorial.CurrentGuide);
        }

        [Test]
        public void Only_a_save_without_progress_shows_the_tutorial()
        {
            Assert.IsTrue(TutorialMode.ShouldShow(new SaveData()));
            Assert.IsFalse(TutorialMode.ShouldShow(new SaveData { TutorialDone = true }));
            Assert.IsFalse(TutorialMode.ShouldShow(new SaveData { HasGame = true }));
            Assert.IsFalse(TutorialMode.ShouldShow(new SaveData { NextGameIndex = 1 }));
            Assert.IsFalse(TutorialMode.ShouldShow(new SaveData { BestScore = 10 }));
            var dailyPlayed = new SaveData();
            dailyPlayed.Daily.Record.LastDay = 20746;
            Assert.IsFalse(TutorialMode.ShouldShow(dailyPlayed));
            var dailyInProgress = new SaveData();
            dailyInProgress.Daily.HasGame = true;
            Assert.IsFalse(TutorialMode.ShouldShow(dailyInProgress));
        }
    }
}
