#nullable enable
using System;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The game screen: builds every view in code, resumes the saved game or starts a new one, saves after every
    /// placement, pauses, and shows game over with Home. Its GameMode decides which game it plays and how it is recorded:
    /// Classic offers Play again, the Daily offers Share. It asks for Home through HomeRequested and never navigates
    /// itself. Everything it creates is a child of its own object.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        SaveStore? _store;
        SaveData _save = new SaveData();
        GameMode _mode = null!;
        Action<string>? _share;
        SettingsData _settings = new SettingsData();
        GameSession? _game;
        PointerInput? _input;
        bool _booted;

        /// <summary>Raised by Home on the pause overlay and on game over.</summary>
        public event Action? HomeRequested;

        /// <summary>Raised when a guided mode, the tutorial, ends after its last step or on Skip.</summary>
        public event Action? Completed;

        /// <summary>Raised just before a rewarded ad shows, so App can space interstitials after it.</summary>
        public event Action? FullScreenAdShown;

        /// <summary>The ad service for the rewarded revive; set by App. Without one, no revive is offered.</summary>
        public IAdService? Ads { get; set; }

        /// <summary>
        /// Runs between games, when the player leaves game over through Play again or Home, with what to do next. App
        /// shows an interstitial here when AdPolicy allows; without it, the next step simply runs.
        /// </summary>
        public Action<Action>? BetweenGames { get; set; }

        bool _pendingFinish;   // game over offers a revive, so the game isn't recorded as finished until the player leaves

        /// <summary>The move the player is guided to make next, or null for free play.</summary>
        public Guide? CurrentGuide => _booted ? _mode.CurrentGuide : null;

        /// <summary>The tutorial's hint and Skip, and its hand; null outside a guided mode.</summary>
        public TutorialHintView? Hint { get; private set; }
        public HandMarker? Hand { get; private set; }

        public GameSession Game => _game ?? throw new InvalidOperationException("Call Boot first.");
        public BoardLayout Layout { get; private set; }
        public BoardView Board { get; private set; } = null!;
        public TrayView Tray { get; private set; } = null!;
        public DragController Drag { get; private set; } = null!;
        public HudView Hud { get; private set; } = null!;
        public PauseView Pause { get; private set; } = null!;
        public GameOverView GameOver { get; private set; } = null!;
        public FeedbackPlayer Feedback { get; private set; } = null!;
        public bool IsPaused { get; private set; }

        const float BoardHalfCells = BlockPuzzle.Core.Board.Size / 2f;   // the board's middle, in cells from its top

        /// <summary>The band at the top of the safe area, in world units, kept free for the banner ad.</summary>
        public float BannerHeight { get; private set; }

        void Start()
        {
            if (!_booted) Boot(SaveStore.DefaultPath);
        }

        void OnDisable()
        {
            // Hidden mid-drag (Home, or another screen): the piece goes back and nothing is placed.
            if (_booted && Drag != null) Drag.Cancel();
        }

        // A call, another app or the notification shade: the player comes back to the pause overlay, not a running
        // game. Coming back does nothing; only Resume resumes. A hidden game gets neither message.
        void OnApplicationPause(bool paused)
        {
            if (paused) PauseForInterruption();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) PauseForInterruption();
        }

        void PauseForInterruption()
        {
            if (_booted && !IsPaused && !GameOver.IsShown) PauseGame();
        }

        /// <summary>Builds the game against its own save file. Start calls it with the default path unless something called Boot first.</summary>
        public void Boot(string savePath)
        {
            if (_booted) return;
            var store = new SaveStore(savePath);
            Boot(store, store.TryRead(out var saved) ? saved : new SaveData());
        }

        /// <summary>Builds a Classic game against a store and the data read from it.</summary>
        public void Boot(SaveStore store, SaveData save)
        {
            if (_booted) return;
            Boot(store, save, new ClassicMode(save));
        }

        /// <summary>
        /// Builds the game against a store and the data read from it; App calls this, so every screen shares one SaveData.
        /// share posts the Daily's result (DailyShare.Share unless a test gives its own). settings is App's own SettingsData,
        /// so a toggle changed in Settings takes effect on the next move; without it, every effect is on.
        /// </summary>
        public void Boot(SaveStore store, SaveData save, GameMode mode, Action<string>? share = null, SettingsData? settings = null)
        {
            if (_booted) return;
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("GameController needs a camera tagged MainCamera.");
            _booted = true;
            Application.targetFrameRate = 60;
            _store = store;
            _save = save;
            _mode = mode;
            _share = share;
            if (settings != null) _settings = settings;

            BannerHeight = BannerWorldHeight(camera);
            Layout = new BoardLayout(SafeWorldRect(camera), BannerHeight);
            Board = Child<BoardView>("Board");
            Board.Build(Layout);
            Tray = Child<TrayView>("Tray");
            Tray.Build(Layout);

            // The HUD sits exactly over the score band the layout reserves, which is already inside the safe area.
            var canvas = UiFactory.Canvas(transform, "Canvas");
            Hud = HudView.Create(canvas, ScreenFraction(camera, Layout.BoardTopLeft.y), ScreenFraction(camera, Layout.ScoreBandTop), TogglePause);
            var safeArea = SafeAreaPanel.Create(canvas);
            DebugOverlay.CreateIfDevelopment(safeArea);   // development builds only
            if (mode.CurrentGuide != null) Hint = TutorialHintView.Create(safeArea, SkipGuided);
            float boardMiddle = ScreenFraction(camera, Layout.BoardTopLeft.y - BoardHalfCells * Layout.CellSize);
            var praise = UiFactory.Outlined(UiFactory.FittedText(UiFactory.Rect(canvas, "Praise", new Vector2(0.5f, boardMiddle), new Vector2(900, 200), Vector2.zero),
                110, Color.white));
            Pause = PauseView.Create(canvas, TogglePause, RequestHome);
            GameOver = GameOverView.Create(canvas, mode.Title, mode.CanPlayAgain ? () => LeaveGameOver(PlayAgain) : (Action?)null,
                mode.CanShare ? ShareResult : (Action?)null, () => LeaveGameOver(RequestHome), mode.CanRevive ? ReviveWithAd : (Action?)null);
            GameOver.Hide();
            UiFactory.EnsureEventSystem(transform);   // App already has one; a GameController on its own needs its own

            Drag = gameObject.AddComponent<DragController>();
            Drag.Init(Layout, Board, Tray, () => Game, OnPlaced);
            _input = gameObject.AddComponent<PointerInput>();
            _input.Init(camera, Drag);
            Feedback = gameObject.AddComponent<FeedbackPlayer>();
            Feedback.Init(Board, Layout, praise);
            if (mode.CurrentGuide != null) Hand = HandMarker.Create(transform, Layout.CellSize, () => Drag.IsDragging || Drag.IsReturning || IsPaused);

            _game = mode.Resume();
            if (_game == null)
            {
                StartNextGame();
            }
            else
            {
                RenderAll();
                if (_game.IsGameOver) FinishGame();   // a save made on the last move, before the game-over save
            }
            ApplyGuide();
        }

        /// <summary>Points the drag, the hint and the hand at the guided move, or lets pieces land anywhere when there is none.</summary>
        void ApplyGuide()
        {
            var guide = _mode.CurrentGuide;
            if (guide == null)
            {
                Drag.SetGuide(-1, -1);
                return;
            }
            var g = guide.Value;
            Drag.SetGuide(g.Row, g.Col);
            if (Hint != null) Hint.Show(g.Hint);
            var shape = Game.GetTrayPiece(g.Slot);
            if (Hand != null && shape != null) Hand.Point(Layout.TraySlotCenter(g.Slot), Layout.PointerFor(g.Row, g.Col, shape.Width, shape.Height));
        }

        void SkipGuided()
        {
            _mode.Skip();
            CompleteGuided();
        }

        void CompleteGuided()
        {
            if (Hand != null) Hand.Hide();
            Save();   // the tutorial marked itself done in the save data
            Completed?.Invoke();
        }

        /// <summary>Starts the next game; the Play again button calls this.</summary>
        public void PlayAgain()
        {
            RecordPendingFinish();
            GameOver.Hide();
            StartNextGame();
        }

        /// <summary>Play again or Home on the game-over panel: records the game, then lets App put an interstitial in between.</summary>
        void LeaveGameOver(Action next)
        {
            RecordPendingFinish();
            if (BetweenGames != null) BetweenGames(next);
            else next();
        }

        void RecordPendingFinish()
        {
            if (!_pendingFinish) return;
            _pendingFinish = false;
            GameOver.ShowRevive(false);
            _mode.Finished(Game);
            Save();
        }

        /// <summary>The revive button: once per game, whatever the ad does. A skipped or failed ad leaves game over as it was.</summary>
        void ReviveWithAd()
        {
            if (Ads == null || !_pendingFinish) return;
            GameOver.ShowRevive(false);
            FullScreenAdShown?.Invoke();
            Ads.ShowRewarded(rewarded =>
            {
                if (!rewarded || !Game.TryRevive()) return;   // TryRevive deals a fresh tray that fits
                _pendingFinish = false;
                GameOver.Hide();
                RenderAll();
                SaveGameInProgress();
            });
        }

        /// <summary>The pause button and the back button: pauses or resumes. Ignored on game over.</summary>
        public void TogglePause()
        {
            if (!_booted) return;
            if (IsPaused) Unpause();
            else if (!GameOver.IsShown) PauseGame();
        }

        void PauseGame()
        {
            Drag.Cancel();   // the pause button pressed with another finger mid-drag
            IsPaused = true;
            if (_input != null) _input.enabled = false;   // the board stays visible behind the overlay, but takes no input
            Pause.Show();
        }

        void Unpause()
        {
            IsPaused = false;
            if (_input != null) _input.enabled = true;
            Pause.Hide();
        }

        /// <summary>Home on the pause overlay or on game over. The game comes back unpaused.</summary>
        void RequestHome()
        {
            if (IsPaused) Unpause();
            HomeRequested?.Invoke();
        }

        void StartNextGame()
        {
            _game = _mode.StartNew();
            RenderAll();
            SaveGameInProgress();
        }

        void OnPlaced(PlacementOutcome outcome, ulong placedMask)
        {
            var cue = FeedbackRules.For(outcome, _settings);
            if (_mode.CurrentGuide != null)
            {
                // A guided move. The next step's game replaces the tray the engine just dealt before anything shows it.
                var next = _mode.NextStep(Game);
                if (next == null)
                {
                    CompleteGuided();
                    return;
                }
                _game = next;
                RenderAll();
                Feedback.Play(cue, placedMask, outcome.ClearedMask);
                ApplyGuide();
                return;
            }
            RenderAll();
            Feedback.Play(cue, placedMask, outcome.ClearedMask);
            if (Game.IsGameOver) FinishGame();
            else SaveGameInProgress();
        }

        /// <summary>Records the finished game in the mode, shows game over, then saves.</summary>
        void FinishGame()
        {
            var game = Game;
            bool offerRevive = _mode.CanRevive && !game.Revived && Ads != null && Ads.IsRewardedReady;
            if (offerRevive)
            {
                // A revive would continue this game, so it's saved as it stands and recorded as finished only when the
                // player leaves game over. Killed here, the app comes back to this game over.
                _pendingFinish = true;
                _mode.InProgress(game);
                GameOver.Show(game.Score, Math.Max(game.Score, _mode.Best), _mode.Streak);
            }
            else
            {
                _mode.Finished(game);
                GameOver.Show(game.Score, _mode.Best, _mode.Streak);
            }
            GameOver.ShowRevive(offerRevive);
            Save();
        }

        void SaveGameInProgress()
        {
            _mode.InProgress(Game);
            Save();
        }

        /// <summary>The Daily's Share button: posts the finished game's result.</summary>
        void ShareResult() => (_share ?? DailyShare.Share)(_mode.ShareText(Game));

        /// <summary>A failed save (full storage, a file another app holds) must not stop the game; the next move tries again.</summary>
        void Save()
        {
            try
            {
                _store?.Write(_save);
            }
            catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Couldn't save the game: " + e.Message);
            }
        }

        void RenderAll()
        {
            var game = Game;
            Board.Render(game.Board);
            Tray.Render(game);
            Hud.Show(game.Score, Math.Max(game.Score, _mode.Best));
            Hud.ShowStreak(game.Streak);
        }

        T Child<T>(string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }

        /// <summary>The banner slot in world units: BannerSlot's pixels as a share of the screen height. Nothing without a screen.</summary>
        static float BannerWorldHeight(Camera camera)
        {
            if (Screen.width <= 0 || Screen.height <= 0) return 0f;
            return BannerSlot.HeightPixels(Screen.dpi, Screen.width) / Screen.height * camera.orthographicSize * 2;
        }

        /// <summary>A world y as a fraction of the screen height, from the bottom. The camera's view is the whole screen.</summary>
        static float ScreenFraction(Camera camera, float worldY)
        {
            float viewBottom = camera.transform.position.y - camera.orthographicSize;
            return (worldY - viewBottom) / (camera.orthographicSize * 2);
        }

        /// <summary>The safe area in world units: the camera's view, minus notches. Falls back to 9:16 when there is no screen.</summary>
        static Rect SafeWorldRect(Camera camera)
        {
            float height = camera.orthographicSize * 2;
            bool hasScreen = Screen.width > 0 && Screen.height > 0;
            float width = height * (hasScreen ? (float)Screen.width / Screen.height : 9f / 16f);
            Vector2 centre = camera.transform.position;
            var view = new Rect(centre.x - width / 2, centre.y - height / 2, width, height);
            if (!hasScreen) return view;

            Rect safe = Screen.safeArea;
            return new Rect(
                view.x + view.width * safe.x / Screen.width,
                view.y + view.height * safe.y / Screen.height,
                view.width * safe.width / Screen.width,
                view.height * safe.height / Screen.height);
        }
    }
}
