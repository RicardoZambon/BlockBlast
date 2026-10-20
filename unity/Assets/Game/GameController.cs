#nullable enable
using System;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The game screen: builds every view in code, resumes the saved game or starts a new one, saves after every
    /// placement, pauses, and shows game over with Play again and Home. It asks for Home through HomeRequested and never
    /// navigates itself. Everything it creates is a child of its own object.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        SaveStore? _store;
        SaveData _save = new SaveData();
        GameSession? _game;
        PointerInput? _input;
        bool _booted;

        /// <summary>Raised by Home on the pause overlay and on game over.</summary>
        public event Action? HomeRequested;

        public GameSession Game => _game ?? throw new InvalidOperationException("Call Boot first.");
        public BoardLayout Layout { get; private set; }
        public BoardView Board { get; private set; } = null!;
        public TrayView Tray { get; private set; } = null!;
        public DragController Drag { get; private set; } = null!;
        public HudView Hud { get; private set; } = null!;
        public PauseView Pause { get; private set; } = null!;
        public GameOverView GameOver { get; private set; } = null!;
        public bool IsPaused { get; private set; }

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

        /// <summary>Builds the game against a store and the data read from it. App calls this, so both share one SaveData.</summary>
        public void Boot(SaveStore store, SaveData save)
        {
            if (_booted) return;
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("GameController needs a camera tagged MainCamera.");
            _booted = true;
            Application.targetFrameRate = 60;
            _store = store;
            _save = save;

            BannerHeight = BannerWorldHeight(camera);
            Layout = new BoardLayout(SafeWorldRect(camera), BannerHeight);
            Board = Child<BoardView>("Board");
            Board.Build(Layout);
            Tray = Child<TrayView>("Tray");
            Tray.Build(Layout);

            // The HUD sits exactly over the score band the layout reserves, which is already inside the safe area.
            var canvas = UiFactory.Canvas(transform, "Canvas");
            Hud = HudView.Create(canvas, ScreenFraction(camera, Layout.BoardTopLeft.y), ScreenFraction(camera, Layout.ScoreBandTop), TogglePause);
            DebugOverlay.CreateIfDevelopment(SafeAreaPanel.Create(canvas));   // development builds only
            Pause = PauseView.Create(canvas, TogglePause, RequestHome);
            GameOver = GameOverView.Create(canvas, PlayAgain, RequestHome);
            GameOver.Hide();
            UiFactory.EnsureEventSystem(transform);   // App already has one; a GameController on its own needs its own

            Drag = gameObject.AddComponent<DragController>();
            Drag.Init(Layout, Board, Tray, () => Game, OnPlaced);
            _input = gameObject.AddComponent<PointerInput>();
            _input.Init(camera, Drag);

            // A damaged save can hold impossible counters; the GameSession constructor would refuse them on every launch.
            _save.NextGameIndex = Math.Max(0, _save.NextGameIndex);
            _save.NextRecentQuickLosses = Math.Max(0, _save.NextRecentQuickLosses);
            _save.BestScore = Math.Max(0, _save.BestScore);

            _game = _save.HasGame ? Resume(_save.Game) : null;
            if (_game == null)
            {
                StartNextGame();
                return;
            }
            RenderAll();
            if (_game.IsGameOver) FinishGame();   // a save made on the last move, before the game-over save
        }

        /// <summary>Starts the next game; the Play again button calls this.</summary>
        public void PlayAgain()
        {
            GameOver.Hide();
            StartNextGame();
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
            _game = new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), SeedSource.NewSeed(),
                _save.NextGameIndex, _save.NextRecentQuickLosses);
            RenderAll();
            SaveGameInProgress();
        }

        /// <summary>Whether a save holds a game that resumes and isn't over: what Home offers as Continue.</summary>
        public static bool CanResume(SaveData save)
        {
            if (!save.HasGame) return false;
            var game = Resume(save.Game);
            return game != null && !game.IsGameOver;
        }

        static GameSession? Resume(GameSnapshot snapshot)
        {
            try
            {
                return GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings());
            }
            catch (InvalidSnapshotException)
            {
                return null;   // a save we can't resume: start a new game, keeping the counters
            }
        }

        void OnPlaced(PlacementOutcome outcome)
        {
            RenderAll();
            if (Game.IsGameOver) FinishGame();
            else SaveGameInProgress();
        }

        /// <summary>Shows game over, then saves what the next game starts with.</summary>
        void FinishGame()
        {
            var game = Game;
            if (game.Score > _save.BestScore) _save.BestScore = game.Score;
            GameOver.Show(game.Score, _save.BestScore);
            _save.HasGame = false;
            _save.NextGameIndex = game.GameIndex + 1;
            _save.NextRecentQuickLosses = game.NextRecentQuickLosses;
            Save();
        }

        void SaveGameInProgress()
        {
            if (Game.Score > _save.BestScore) _save.BestScore = Game.Score;
            _save.HasGame = true;
            _save.Game = Game.ToSnapshot();
            Save();
        }

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
            Hud.Show(game.Score, Math.Max(game.Score, _save.BestScore));
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
