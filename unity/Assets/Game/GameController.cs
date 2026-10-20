#nullable enable
using System;
using BlockPuzzle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The composition root: builds every view in code, resumes the saved game or starts a new one, saves after every
    /// placement, and shows game over with Play again. Everything it creates is a child of its own object.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        SaveStore? _store;
        SaveData _save = new SaveData();
        GameSession? _game;
        bool _booted;

        public GameSession Game => _game ?? throw new InvalidOperationException("Call Boot first.");
        public BoardLayout Layout { get; private set; }
        public BoardView Board { get; private set; } = null!;
        public TrayView Tray { get; private set; } = null!;
        public DragController Drag { get; private set; } = null!;
        public HudView Hud { get; private set; } = null!;
        public GameOverView GameOver { get; private set; } = null!;

        void Start()
        {
            if (!_booted) Boot(SaveStore.DefaultPath);
        }

        /// <summary>Builds the game against a save file. Start calls it with the default path unless something called it first.</summary>
        public void Boot(string savePath)
        {
            if (_booted) return;
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("GameController needs a camera tagged MainCamera.");
            _booted = true;
            Application.targetFrameRate = 60;
            _store = new SaveStore(savePath);

            Layout = new BoardLayout(SafeWorldRect(camera));
            Board = Child<BoardView>("Board");
            Board.Build(Layout);
            Tray = Child<TrayView>("Tray");
            Tray.Build(Layout);

            var canvas = CreateCanvas();
            Hud = HudView.Create(canvas);
            GameOver = GameOverView.Create(canvas, PlayAgain);
            GameOver.Hide();
            DebugOverlay.CreateIfDevelopment(canvas);   // development builds only
            EnsureEventSystem();

            Drag = gameObject.AddComponent<DragController>();
            Drag.Init(Layout, Board, Tray, () => Game, OnPlaced);
            gameObject.AddComponent<PointerInput>().Init(camera, Drag);

            if (_store.TryRead(out var saved)) _save = saved;
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

        void StartNextGame()
        {
            _game = new GameSession(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), SeedSource.NewSeed(),
                _save.NextGameIndex, _save.NextRecentQuickLosses);
            RenderAll();
            SaveGameInProgress();
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

        Transform CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
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
