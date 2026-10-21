#nullable enable
using BlockPuzzle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Plays a placement's FeedbackCue: the board's squash and sweep, a burst of squares from cleared cells, the praise
    /// popup, a light shake of the board, haptics and sounds. Particles are pooled at Init, so playing allocates nothing
    /// new for them. The shake moves only the board's squares, never the layout input uses, and settles exactly home.
    /// </summary>
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        const int ParticleCount = 48;
        const float ParticleSeconds = 0.45f;
        const float ParticleSpeedCells = 3f;
        const float ShakeSeconds = 0.2f;
        const float ShakeCells = 0.08f;
        const float PraiseSeconds = 0.9f;
        const float GoldenAngle = 2.39996f;

        BoardView? _board;
        BoardLayout _layout;
        Vector3 _boardHome;
        Text? _praise;
        AudioSource? _audio;

        readonly SpriteRenderer[] _particles = new SpriteRenderer[ParticleCount];
        readonly Vector2[] _velocities = new Vector2[ParticleCount];
        readonly float[] _ages = new float[ParticleCount];
        int _nextParticle;

        float _shakeTime = -1f;
        float _praiseTime = -1f;
        bool _praiseScales;

        public bool IsShaking => _shakeTime >= 0f;

        public bool PraiseShown => _praiseTime >= 0f;

        public string PraiseText => _praise != null ? _praise.text : "";

        public Text? PraiseLabel => _praise;

        public int LiveParticles { get; private set; }

        public int SoundsPlayed { get; private set; }

        public Sound LastSound { get; private set; }

        public Haptic LastHaptic { get; private set; }

        /// <param name="praise">A text over the board, kept hidden until there is praise.</param>
        public void Init(BoardView board, BoardLayout layout, Text praise)
        {
            _board = board;
            _layout = layout;
            _boardHome = board.transform.localPosition;
            _praise = praise;
            _praise.gameObject.SetActive(false);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;

            for (int i = 0; i < ParticleCount; i++)
            {
                var go = new GameObject("Particle " + i);
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * (layout.CellSize * 0.18f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = CellSprite.Square;
                renderer.color = Palette.Clearing;
                renderer.sortingOrder = 50;
                go.SetActive(false);
                _particles[i] = renderer;
                _ages[i] = -1f;
            }
        }

        public void Play(in FeedbackCue cue, ulong placedMask, ulong clearedMask)
        {
            if (_board == null) return;
            if (cue.Squash) _board.Squash(placedMask);
            if (cue.Sweep) _board.Sweep(clearedMask, delays: !cue.ReducedMotion);
            if (cue.Particles) Burst(clearedMask);
            if (cue.Shake) _shakeTime = 0f;
            if (cue.Praise != null) ShowPraise(cue.Praise, scales: !cue.ReducedMotion);

            LastSound = cue.Sound;
            if (cue.Sound != Sound.None)
            {
                var clip = SoundBank.Clip(cue.Sound);
                if (clip != null && _audio != null)
                {
                    _audio.pitch = cue.Pitch;
                    _audio.PlayOneShot(clip);
                }
                SoundsPlayed++;
            }

            LastHaptic = cue.Haptic;
            Haptics.Play(cue.Haptic);
        }

        /// <summary>Two squares from each cleared cell, at angles spread by the golden angle, so no random numbers are needed.</summary>
        void Burst(ulong clearedMask)
        {
            for (int i = 0; i < Board.Size * Board.Size; i++)
            {
                if ((clearedMask & (1UL << i)) == 0) continue;
                Vector2 centre = _layout.CellCenter(i / Board.Size, i % Board.Size);
                for (int k = 0; k < 2; k++)
                {
                    int p = _nextParticle;
                    _nextParticle = (_nextParticle + 1) % ParticleCount;
                    float angle = (i * 2 + k) * GoldenAngle;
                    _velocities[p] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ParticleSpeedCells * _layout.CellSize * (0.6f + 0.4f * k));
                    _ages[p] = 0f;
                    _particles[p].transform.position = centre;
                    _particles[p].gameObject.SetActive(true);
                }
            }
            CountParticles();
        }

        void ShowPraise(string praise, bool scales)
        {
            if (_praise == null) return;
            _praise.text = praise;
            _praise.gameObject.SetActive(true);
            _praiseTime = 0f;
            _praiseScales = scales;
            var color = _praise.color;
            color.a = 1f;
            _praise.color = color;
            float scale = scales ? 0.8f : 1f;
            _praise.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            for (int p = 0; p < ParticleCount; p++)
            {
                if (_ages[p] < 0f) continue;
                _ages[p] += dt;
                var renderer = _particles[p];
                if (_ages[p] >= ParticleSeconds)
                {
                    _ages[p] = -1f;
                    renderer.gameObject.SetActive(false);
                    continue;
                }
                renderer.transform.position += (Vector3)(_velocities[p] * dt);
                var color = Palette.Clearing;
                color.a = 1f - _ages[p] / ParticleSeconds;
                renderer.color = color;
            }
            CountParticles();

            if (_board != null && _shakeTime >= 0f)
            {
                _shakeTime += dt;
                if (_shakeTime >= ShakeSeconds)
                {
                    _shakeTime = -1f;
                    _board.transform.localPosition = _boardHome;
                }
                else
                {
                    float strength = ShakeCells * _layout.CellSize * (1f - _shakeTime / ShakeSeconds);
                    _board.transform.localPosition = _boardHome + new Vector3(Mathf.Sin(_shakeTime * 90f), Mathf.Cos(_shakeTime * 70f), 0f) * strength;
                }
            }

            if (_praise != null && _praiseTime >= 0f)
            {
                _praiseTime += dt;
                float t = _praiseTime / PraiseSeconds;
                if (t >= 1f)
                {
                    _praiseTime = -1f;
                    _praise.gameObject.SetActive(false);
                }
                else
                {
                    var color = _praise.color;
                    color.a = t < 0.7f ? 1f : (1f - t) / 0.3f;
                    _praise.color = color;
                    float scale = _praiseScales ? 0.8f + 0.2f * Mathf.Min(1f, t / 0.15f) : 1f;
                    _praise.rectTransform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        void CountParticles()
        {
            int live = 0;
            for (int p = 0; p < ParticleCount; p++) if (_ages[p] >= 0f) live++;
            LiveParticles = live;
        }
    }
}
