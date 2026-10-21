#nullable enable
using System;
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    public enum Haptic
    {
        None,
        Light,
        Medium,
        Heavy,
    }

    public enum Sound
    {
        None,
        Place,
        Clear,
        BoardClear,
    }

    /// <summary>What to play for one placement. FeedbackPlayer plays it; FeedbackRules decides it.</summary>
    public readonly struct FeedbackCue
    {
        public readonly Haptic Haptic;
        public readonly Sound Sound;
        public readonly float Pitch;
        public readonly bool Squash;
        public readonly bool Sweep;
        public readonly bool Particles;
        public readonly bool Shake;
        public readonly bool ReducedMotion;
        public readonly string? Praise;
        public readonly int Streak;

        public FeedbackCue(Haptic haptic, Sound sound, float pitch, bool squash, bool sweep, bool particles, bool shake,
            bool reducedMotion, string? praise, int streak)
        {
            Haptic = haptic;
            Sound = sound;
            Pitch = pitch;
            Squash = squash;
            Sweep = sweep;
            Particles = particles;
            Shake = shake;
            ReducedMotion = reducedMotion;
            Praise = praise;
            Streak = streak;
        }
    }

    /// <summary>The feedback for a placement, from its outcome and the player's toggles. Pure, so every rule is tested.</summary>
    public static class FeedbackRules
    {
        const float PitchStep = 0.06f;
        const float MaxPitch = 1.6f;

        public static FeedbackCue For(PlacementOutcome outcome, SettingsData settings)
        {
            bool cleared = outcome.Lines > 0;
            bool motion = !settings.ReducedMotion;
            bool big = outcome.Lines >= 2 || outcome.BoardCleared;

            Haptic haptic = !settings.Haptics ? Haptic.None
                : outcome.BoardCleared ? Haptic.Heavy
                : cleared ? Haptic.Medium
                : Haptic.Light;
            Sound sound = !settings.Sound ? Sound.None
                : outcome.BoardCleared ? Sound.BoardClear
                : cleared ? Sound.Clear
                : Sound.Place;
            float pitch = cleared ? Math.Min(MaxPitch, 1f + PitchStep * Math.Max(0, outcome.Streak - 1)) : 1f;

            return new FeedbackCue(haptic, sound, pitch, squash: motion, sweep: cleared, particles: cleared && motion,
                shake: big && motion, reducedMotion: !motion, Praise(outcome), outcome.Streak);
        }

        static string? Praise(PlacementOutcome outcome)
        {
            if (outcome.BoardCleared) return Strings.Get("praise.board_clear");
            if (outcome.Lines >= 4) return Strings.Get("praise.incredible");
            if (outcome.Lines == 3) return Strings.Get("praise.amazing");
            if (outcome.Lines == 2) return Strings.Get("praise.great");
            if (outcome.Lines == 1 && outcome.Streak >= 3) return Strings.Format("praise.streak", outcome.Streak);
            return null;
        }
    }
}
