#nullable enable
using BlockPuzzle.Core;
using NUnit.Framework;

namespace BlockPuzzle.Game.Tests
{
    public class FeedbackRulesTests
    {
        static PlacementOutcome Outcome(int lines = 0, int streak = 0, bool boardCleared = false) =>
            new PlacementOutcome(points: 10, lines, clearedMask: lines > 0 ? 0xFFUL : 0, boardCleared, streak, newTrayDealt: false, gameOver: false);

        static readonly SettingsData AllOn = new SettingsData();

        [Test]
        public void A_placement_without_a_clear_squashes_and_taps_lightly()
        {
            var cue = FeedbackRules.For(Outcome(), AllOn);

            Assert.AreEqual(Haptic.Light, cue.Haptic);
            Assert.AreEqual(Sound.Place, cue.Sound);
            Assert.IsTrue(cue.Squash);
            Assert.IsFalse(cue.Sweep);
            Assert.IsFalse(cue.Particles);
            Assert.IsFalse(cue.Shake);
            Assert.IsNull(cue.Praise);
        }

        [Test]
        public void A_clear_sweeps_bursts_and_buzzes_medium()
        {
            var cue = FeedbackRules.For(Outcome(lines: 1, streak: 1), AllOn);

            Assert.AreEqual(Haptic.Medium, cue.Haptic);
            Assert.AreEqual(Sound.Clear, cue.Sound);
            Assert.IsTrue(cue.Sweep);
            Assert.IsTrue(cue.Particles);
            Assert.IsFalse(cue.Shake, "one line is not a big clear");
            Assert.IsNull(cue.Praise);
        }

        [TestCase(2, "Great!")]
        [TestCase(3, "Amazing!")]
        [TestCase(4, "Incredible!")]
        [TestCase(6, "Incredible!")]
        public void Big_clears_shake_and_get_praise(int lines, string praise)
        {
            var cue = FeedbackRules.For(Outcome(lines, streak: 1), AllOn);

            Assert.IsTrue(cue.Shake);
            Assert.AreEqual(praise, cue.Praise);
        }

        [Test]
        public void A_board_clear_is_heavy_with_its_own_praise()
        {
            var cue = FeedbackRules.For(Outcome(lines: 1, streak: 1, boardCleared: true), AllOn);

            Assert.AreEqual(Haptic.Heavy, cue.Haptic);
            Assert.AreEqual(Sound.BoardClear, cue.Sound);
            Assert.IsTrue(cue.Shake);
            Assert.AreEqual("Board clear!", cue.Praise);
        }

        [TestCase(1, 1f)]
        [TestCase(2, 1.06f)]
        [TestCase(5, 1.24f)]
        [TestCase(30, 1.6f)]
        public void The_pitch_rises_with_the_streak_and_stops_at_1_6(int streak, float pitch)
        {
            Assert.AreEqual(pitch, FeedbackRules.For(Outcome(lines: 1, streak), AllOn).Pitch, 1e-4f);
            Assert.AreEqual(1f, FeedbackRules.For(Outcome(streak: streak), AllOn).Pitch, 1e-4f, "a placement without a clear keeps its pitch");
        }

        [Test]
        public void A_long_streak_of_single_clears_gets_streak_praise()
        {
            Assert.IsNull(FeedbackRules.For(Outcome(lines: 1, streak: 2), AllOn).Praise);
            Assert.AreEqual("Streak 3!", FeedbackRules.For(Outcome(lines: 1, streak: 3), AllOn).Praise);
            Assert.AreEqual(3, FeedbackRules.For(Outcome(lines: 1, streak: 3), AllOn).Streak);
        }

        [Test]
        public void Reduced_motion_keeps_only_fades()
        {
            var cue = FeedbackRules.For(Outcome(lines: 3, streak: 4), new SettingsData { ReducedMotion = true });

            Assert.IsFalse(cue.Squash);
            Assert.IsFalse(cue.Particles);
            Assert.IsFalse(cue.Shake);
            Assert.IsTrue(cue.Sweep, "the sweep stays, as a plain fade");
            Assert.IsTrue(cue.ReducedMotion);
            Assert.AreEqual("Amazing!", cue.Praise);
        }

        [Test]
        public void Sound_and_haptics_off_play_nothing()
        {
            var cue = FeedbackRules.For(Outcome(lines: 2, streak: 2), new SettingsData { Sound = false, Haptics = false });

            Assert.AreEqual(Sound.None, cue.Sound);
            Assert.AreEqual(Haptic.None, cue.Haptic);
            Assert.IsTrue(cue.Sweep, "visuals don't depend on sound");
        }
    }
}
