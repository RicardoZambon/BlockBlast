#nullable enable
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Development builds only: frame rate, the slowest frame of the last 5 s and how many frames in it allocated, the
    /// determinism check and a dealing benchmark, for the on-device checks. Its text changes once a second, which
    /// allocates; the game itself doesn't, and the count leaves those frames out.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        const float SlowestWindowSeconds = 5f;

        Text? _text;
        string _determinism = "";
        string _bench = "tap Bench to time 300 deals";
        float _secondTimer;
        int _framesThisSecond;
        float _fps;
        float _slowestThisWindow;
        float _slowestShown;
        float _windowTimer;
        ProfilerRecorder _gcAllocated;
        readonly AllocationTally _allocations = new AllocationTally();
        bool _refreshedLastFrame;

        /// <param name="parent">The safe-area panel, so nothing sits under a notch.</param>
        public static DebugOverlay? CreateIfDevelopment(Transform parent)
        {
            if (!Debug.isDebugBuild) return null;

            var panel = UiFactory.Rect(parent, "Debug overlay", new Vector2(0f, 0f), new Vector2(760, 210), new Vector2(400, 130));
            var overlay = panel.gameObject.AddComponent<DebugOverlay>();
            overlay._text = UiFactory.Text(panel, 30, new Color(1f, 0.85f, 0.3f, 1f));
            overlay._text.alignment = TextAnchor.LowerLeft;
            overlay._determinism = EngineChecks.CheckDeterminism();
            overlay._gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            // Top right, in the banner band, which stays empty until Phase 3: PointerInput reads the pointer directly, so a
            // button over the tray would also pick up a piece.
            var button = UiFactory.Rect(parent, "Bench", new Vector2(1f, 1f), new Vector2(220, 100), new Vector2(-140, -70));
            button.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            button.gameObject.AddComponent<Button>().onClick.AddListener(overlay.RunBenchmark);
            UiFactory.Text(UiFactory.Stretch(button, "Label"), 40, Color.white).text = "Bench";
            return overlay;
        }

        void RunBenchmark() => _bench = EngineChecks.BenchmarkDeals(300);

        void OnDestroy() => _gcAllocated.Dispose();

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // The recorder holds the last finished frame, so a refresh is left out one frame later.
            if (_gcAllocated.Valid) _allocations.Add(_gcAllocated.LastValue, _refreshedLastFrame);
            _refreshedLastFrame = false;
            _framesThisSecond++;
            _secondTimer += dt;
            _windowTimer += dt;
            if (dt > _slowestThisWindow) _slowestThisWindow = dt;
            if (_windowTimer >= SlowestWindowSeconds)
            {
                _slowestShown = _slowestThisWindow;
                _slowestThisWindow = 0f;
                _windowTimer = 0f;
                _allocations.EndWindow();
            }
            if (_secondTimer < 1f) return;

            _fps = _framesThisSecond / _secondTimer;
            _framesThisSecond = 0;
            _secondTimer = 0f;
            if (_text != null)
            {
                float slowestMs = Mathf.Max(_slowestShown, _slowestThisWindow) * 1000f;
                _text.text = $"{_fps:F0} fps, slowest frame {slowestMs:F0} ms\n" +
                    $"GC: {_allocations.Frames} frames allocated in 5 s, largest {_allocations.LargestBytes} B\n{_determinism}\n{_bench}";
                _refreshedLastFrame = true;
            }
        }
    }
}
