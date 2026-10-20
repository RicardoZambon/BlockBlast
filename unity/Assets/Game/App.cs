#nullable enable
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockPuzzle.Game
{
    public enum AppScreen
    {
        Home,
        Game,
        Settings,
    }

    /// <summary>
    /// The scene's root: owns the save and the settings, and shows one screen at a time. The game screen is built the
    /// first time Play is pressed, and keeps its state while another screen is shown.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        SaveStore? _saveStore;
        SaveData _save = new SaveData();
        SettingsStore? _settingsStore;
        SettingsData _settings = new SettingsData();
        bool _booted;

        public HomeScreen Home { get; private set; } = null!;
        public SettingsScreen Settings { get; private set; } = null!;
        public GameController? Game { get; private set; }
        public AppScreen Current { get; private set; }

        void Start()
        {
            if (!_booted) Boot(Application.persistentDataPath);
        }

        void Update()
        {
            // The Android back button arrives as Escape.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
        }

        /// <summary>Builds the app with its save and settings files in folder. Start calls it unless something called it first.</summary>
        public void Boot(string folder)
        {
            if (_booted) return;
            _booted = true;
            Application.targetFrameRate = 60;
            _saveStore = new SaveStore(Path.Combine(folder, SaveStore.FileName));
            if (_saveStore.TryRead(out var saved)) _save = saved;
            _settingsStore = new SettingsStore(Path.Combine(folder, SettingsStore.FileName));
            _settings = _settingsStore.Load();

            UiFactory.EnsureEventSystem(transform);
            Home = HomeScreen.Create(transform, PlayOrContinue, ShowSettings);
            Settings = SettingsScreen.Create(transform, _settings, SaveSettings, ShowHome);
            ShowHome();
        }

        public void ShowHome()
        {
            // Once the game screen exists it keeps the shared SaveData current after every move. Before that, only a
            // save that resumes into a game that isn't over is worth a Continue.
            Home.Refresh(Game != null ? _save.HasGame : GameController.CanResume(_save));
            Show(AppScreen.Home);
        }

        /// <summary>Continues the game in progress, or starts the next one, including after a game over.</summary>
        public void PlayOrContinue()
        {
            if (Game == null) Game = CreateGame();
            if (Game.GameOver.IsShown) Game.PlayAgain();   // after a game over, or a saved game that was already over
            Show(AppScreen.Game);
        }

        public void ShowSettings() => Show(AppScreen.Settings);

        /// <summary>The back button: Settings goes back to Home, the game toggles pause, and Home does nothing.</summary>
        public void Back()
        {
            if (Current == AppScreen.Settings) ShowHome();
            else if (Current == AppScreen.Game && Game != null) Game.TogglePause();
        }

        GameController CreateGame()
        {
            var go = new GameObject("Game");
            go.transform.SetParent(transform, false);
            var game = go.AddComponent<GameController>();
            game.Boot(_saveStore!, _save);
            game.HomeRequested += ShowHome;
            return game;
        }

        /// <summary>Shows one screen and hides the others' roots.</summary>
        void Show(AppScreen screen)
        {
            Current = screen;
            Home.gameObject.SetActive(screen == AppScreen.Home);
            Settings.gameObject.SetActive(screen == AppScreen.Settings);
            if (Game != null) Game.gameObject.SetActive(screen == AppScreen.Game);
        }

        void SaveSettings() => _settingsStore?.Save(_settings);
    }
}
