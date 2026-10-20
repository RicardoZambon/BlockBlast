#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Short tones made in code until there are real sounds: a soft tick to place, a two-note chime to clear, and a
    /// rising arpeggio for a board clear. Each is built once, the first time it's asked for.
    /// </summary>
    public static class SoundBank
    {
        const int SampleRate = 44100;

        static AudioClip? _place;
        static AudioClip? _clear;
        static AudioClip? _boardClear;

        public static AudioClip? Clip(Sound sound) => sound switch
        {
            Sound.Place => _place ??= Tone("Place", new[] { 520f }, 0.05f, 0.25f),
            Sound.Clear => _clear ??= Tone("Clear", new[] { 784f, 1046f }, 0.16f, 0.3f),
            Sound.BoardClear => _boardClear ??= Tone("Board clear", new[] { 523f, 659f, 784f, 1046f }, 0.4f, 0.3f),
            _ => null,
        };

        /// <summary>The notes one after another, each a sine with a 4 ms attack and an exponential fade.</summary>
        static AudioClip Tone(string name, float[] notes, float seconds, float volume)
        {
            int samples = (int)(SampleRate * seconds);
            int perNote = samples / notes.Length;
            int attack = SampleRate * 4 / 1000;
            var data = new float[samples];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float envelope = Mathf.Min(1f, i / (float)attack) * Mathf.Exp(-4f * i / perNote);
                    data[n * perNote + i] = volume * envelope * Mathf.Sin(2f * Mathf.PI * notes[n] * i / SampleRate);
                }
            }
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
