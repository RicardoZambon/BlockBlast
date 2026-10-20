# Phase 2a: App Shell (Screens and Layout) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the one-screen prototype into an app with a Home screen, a Game screen with pause, Game over and Settings. Everything sits inside the safe area, with a banner slot reserved from day one so that turning ads on later never moves the board under a finger.

**Architecture:**
- **App:** a new `App` component is the scene's only root. It owns the save store, the settings store and one screen at a time: Home, Game or Settings.
- **Game screen:** the existing `GameController` becomes it, adding a pause button, a pause overlay and a Home button on game over. It raises `HomeRequested` and never navigates itself.
- **Layout:**
  - Every screen's UI goes under a `SafeAreaPanel`, which matches `Screen.safeArea`.
  - The board layout and the HUD both leave a banner band at the top of the safe area.
- **Saves:** a small `JsonFile<T>` holds the atomic-write and tolerant-read code that `SaveStore` already has. The new `SettingsStore` uses it too, so settings and the game save are separate files and one can't corrupt the other.

**Tech Stack:** the same as Task 1.8: Unity 6000.3.26f1, URP 2D, uGUI with legacy `Text`, Input System, Unity Test Framework.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), tasks 2.3 (Screens) and 2.7 (Layout), plus Global Constraints (Ad rules, Platforms). Builds on the [Task 1.8 plan](2026-10-20-task-1.8-unity-prototype.md).

## Global Constraints

- Everything in the [Task 1.8 plan](2026-10-20-task-1.8-unity-prototype.md)'s Global Constraints still holds: C# 9, `#nullable enable`, the namespaces, no `System.Random`, the engine as the only source of rules, the command lines, and keeping Unity Hub running for the license.
- **Screens** (master plan 2.3), in this plan's scope:
  - **Home:** Play, Daily, Settings. Daily stays disabled, labelled "Daily · soon", until task 2.5 builds it.
  - **Game:** score, best score, pause.
  - **Game over:** score, best score, a spot for the revive offer, and Play Again. Home is added too.
  - **Settings:** toggles for sound, music, haptics and reduced motion, and the version number.
    - The toggles are stored now; tasks 2.2 and 2.9 make them do something.
    - Privacy Settings, Restore Purchases, Support and Privacy Policy wait for Phase 3 and task 0.5. Their services and links don't exist yet.
- **Layout** (master plan 2.7):
  - Respect safe areas.
  - Support screen shapes from 4:3 (iPad) to 21:9 (tall Android phones).
  - Reserve the banner slot at the top from day one.
- **Ad rules:** the banner goes at the top of the game screen, never next to the piece tray. Its slot is 50 dp tall on phones and 90 dp on tablets (728 dp wide or more), matching the standard banner and leaderboard sizes.
- **Continue:** Home's main button reads "Continue" when a game is in progress, and "Play" otherwise, including right after a game over.
- **Android back button:** Settings goes back to Home, the game toggles pause, and Home does nothing.

## Review Focus

- **Leaving the game mid-drag** (the pause button pressed with another finger, or the back button): the piece goes back to the tray and nothing is placed. *Task 4:* `GameController` cancels a drag when hidden.
- **Continue after game over:** Home must offer a new game, never resume a finished one. *Task 5:* the Continue/Play label test.
- **A damaged settings file:** the defaults apply and the app still starts; the game save is untouched. *Task 2:* the corrupt-settings test.
- **Notches and safe-area changes:** UI anchors follow `Screen.safeArea` and update when it changes. *Task 1:* `SafeArea.Anchors` tests. *Task 3:* the panel reapplies when the area changes.
- **Tall phones and tablets with the banner band:** at 9:21 and 3:4, the banner band, score, board and tray all fit. *Task 1:* `BoardLayout` tests with a banner height.

## File Structure

| Path | Responsibility |
|---|---|
| `unity/Assets/Game/UI/SafeArea.cs` | Pure: anchors that make a RectTransform cover the safe area |
| `unity/Assets/Game/UI/SafeAreaPanel.cs` | Applies `SafeArea.Anchors` to its RectTransform, and again whenever `Screen.safeArea` changes |
| `unity/Assets/Game/UI/BannerSlot.cs` | Pure: the banner slot's height in pixels for a screen's dpi and width |
| `unity/Assets/Game/Presentation/BoardLayout.cs` | Gains an optional banner height: the band at the top of the safe area that the stack leaves free |
| `unity/Assets/Game/Services/JsonFile.cs` | Atomic JSON write and tolerant read, moved out of `SaveStore` |
| `unity/Assets/Game/Services/SaveStore.cs` | Now a thin wrapper: `JsonFile<SaveData>` plus the version check |
| `unity/Assets/Game/Services/SettingsData.cs`, `SettingsStore.cs` | Sound, music, haptics and reduced motion, in `settings.json` |
| `unity/Assets/Game/UI/HudView.cs` | Score and best inside the safe area below the banner band, plus a pause button |
| `unity/Assets/Game/UI/PauseView.cs` | Pause overlay: Resume, Home |
| `unity/Assets/Game/UI/GameOverView.cs` | Adds Home and an empty revive slot |
| `unity/Assets/Game/UI/HomeScreen.cs`, `SettingsScreen.cs` | The Home and Settings screens, each with its own canvas |
| `unity/Assets/Game/GameController.cs` | The game screen: pause, Home requests, cancelling a drag when hidden, a banner-aware layout. `Boot(SaveStore, SaveData)` lets App share its stores |
| `unity/Assets/Game/App.cs` | Composition root and navigation |
| `unity/Assets/Game/EditorTools/ProjectSetup.cs` | `BuildScene` puts an `App` in the scene instead of a `GameController` |
| `unity/Assets/Game/Tests/EditMode/SafeAreaTests.cs`, `BannerSlotTests.cs`, `SettingsStoreTests.cs` | New |
| `unity/Assets/Game/Tests/EditMode/BoardLayoutTests.cs` | Adds 9:21 and banner cases |
| `unity/Assets/Game/Tests/PlayMode/AppFlowTests.cs` | Home, Play, Continue, pause, game over, Settings, back button |

---

### Task 1: Safe area, banner slot and layout

**Files:**
- Create: `UI/SafeArea.cs`, `UI/BannerSlot.cs`
- Modify: `Presentation/BoardLayout.cs`
- Test: `Tests/EditMode/SafeAreaTests.cs`, `BannerSlotTests.cs`; extend `BoardLayoutTests.cs`

**Interfaces:**
- Produces:
  - `static class SafeArea`:
    - `static void Anchors(Rect safeArea, Vector2 screenSize, out Vector2 anchorMin, out Vector2 anchorMax)`: the safe area as fractions of the screen.
    - A zero-size screen gives (0, 0) and (1, 1).
  - `static class BannerSlot`:
    - `const float PhoneDp = 50f`, `TabletDp = 90f`, `TabletMinWidthDp = 728f`.
    - `static float HeightPixels(float dpi, float screenWidthPixels)`. A dpi of 0 or less counts as 160. The width in dp is `pixels * 160 / dpi`.
  - `BoardLayout(Rect safeWorldRect, float bannerHeight = 0f)`. The stack (score, board, gap, tray: 13.25 cells) fits in the safe rect minus a band of `bannerHeight` world units at the top, and is centred in what's left.
  - `float ScoreBandTop`: the world y of the top of the score band (`BoardTopLeft.y + 1.5 * CellSize`), so the HUD can be placed exactly over it.

- [ ] **Step 1: Write the failing tests.**
  - `SafeAreaTests`:
    - `Anchors_cover_the_safe_area`: on a 1080×2400 screen with a safe area of (0, 96, 1080, 2216), anchorMin is (0, 0.04) and anchorMax is (1, 0.963333) within 1e-5.
    - `The_full_screen_and_a_zero_screen_give_full_anchors`.
  - `BannerSlotTests`:
    - `A_phone_gets_a_50_dp_banner`: dpi 440 at width 1080 is 392 dp wide, so 137.5 px.
    - `A_tablet_gets_a_90_dp_banner`: dpi 264 at width 1536 is 931 dp wide, so 148.5 px.
    - `An_unknown_dpi_counts_as_160`: dpi 0 at width 600 is 600 dp, so 50 px.
  - `BoardLayoutTests`:
    - Add `[TestCase(9f, 21f)]` to every case that loops over aspect ratios.
    - `The_banner_band_stays_clear`: for 9:16, 9:21 and 3:4 with `bannerHeight = 1f`, the score band's top (`BoardTopLeft.y + 1.5 * CellSize`) is at or below `safe.yMax - 1f`, and the tray still fits.
- [ ] **Step 2: Run them and see them fail.** Run EditMode. Expected: exit code 1 ("Scripts have compiler errors").
- [ ] **Step 3: Implement.** `CellSize = Mathf.Min(width * 0.92f / 8, (height - bannerHeight) / 13.25f)`, with the stack centred in `[yMin, yMax - bannerHeight]`.
- [ ] **Step 4: Run EditMode.** Expected: exit code 0 and `failed="0"`.
- [ ] **Step 5: Commit:** "Add safe-area anchors, the banner slot size and a banner-aware board layout".

---

### Task 2: Settings and a shared JSON file

**Files:**
- Create: `Services/JsonFile.cs`, `Services/SettingsData.cs`, `Services/SettingsStore.cs`
- Modify: `Services/SaveStore.cs`
- Test: `Tests/EditMode/SettingsStoreTests.cs`. The existing `SaveStoreTests` must stay green unchanged.

**Interfaces:**
- Produces:
  - `static class JsonFile`:
    - `static void Write<T>(string path, T data)`: write `path.tmp`, flush to disk, then `File.Replace` or `File.Move` into place, creating the folder if needed.
    - `static bool TryRead<T>(string path, out T data) where T : class, new()`: false for a missing file, bad JSON or IO errors; never throws because of content.
  - `SaveStore` keeps its public API and calls these, plus the version check.
  - `[Serializable] sealed class SettingsData`:
    - `int Version = 1` (`const int CurrentVersion = 1`).
    - `bool Sound = true`, `bool Music = true`, `bool Haptics = true`, `bool ReducedMotion`.
  - `sealed class SettingsStore`:
    - `SettingsStore(string path)`.
    - `static string DefaultPath`: `persistentDataPath/settings.json`.
    - `SettingsData Load()`: defaults when the file is missing, bad, or from another version.
    - `void Save(SettingsData)`: logs a warning and carries on when it can't write.

- [ ] **Step 1: Write the failing tests:**
  - `Settings_default_to_sound_music_and_haptics_on_and_reduced_motion_off`
  - `Saved_settings_load_back`
  - `A_corrupt_settings_file_gives_the_defaults`
  - `A_settings_file_from_another_version_gives_the_defaults`
  - `A_failed_settings_save_does_not_throw`: the path is a folder.
- [ ] **Step 2: Run them and see them fail** (compile errors).
- [ ] **Step 3: Implement.** Move the body of `SaveStore.Write` and `SaveStore.TryRead` into `JsonFile`.
- [ ] **Step 4: Run EditMode.** Expected: all green, including the existing `SaveStoreTests`.
- [ ] **Step 5: Commit:** "Store settings in their own file, sharing the atomic JSON code with saves".

---

### Task 3: Safe-area panel, HUD with pause, pause overlay, game-over Home

**Files:**
- Create: `UI/SafeAreaPanel.cs`, `UI/PauseView.cs`
- Modify: `UI/HudView.cs`, `UI/GameOverView.cs`, `UI/UiFactory.cs`
- Test: `Tests/EditMode/ViewTests.cs`. Add UI cases that build these views on a `Canvas` created by the test.

**Interfaces:**
- Produces:
  - `sealed class SafeAreaPanel : MonoBehaviour`:
    - `static RectTransform Create(Transform canvas)`: a stretched child.
    - It applies `SafeArea.Anchors(Screen.safeArea, screen size)` in `Awake`, and again in `Update` whenever the safe area or screen size changes.
  - `HudView.Create(Transform canvas, float bandBottom, float bandTop, Action pause)`:
    - The HUD is stretched across the screen between those two fractions of the screen height: the score band `BoardLayout` reserves, which already sits below the banner and inside the safe area.
    - `PressPause()` is there for tests.
  - `sealed class PauseView : MonoBehaviour`:
    - `static PauseView Create(Transform canvas, Action resume, Action home)`.
    - `Show()`, `Hide()`, `IsShown`, `PressResume()`, `PressHome()`.
  - `GameOverView.Create(Transform canvas, Action playAgain, Action home)`:
    - Adds `PressHome()`.
    - Adds `RectTransform ReviveSlot`, an empty area between the scores and Play again, inactive until Phase 3.
  - `UiFactory.Button(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Vector2 position, Action onClick)`: the same button code `GameOverView` already builds by hand.

- [ ] **Step 1: Write the failing tests:**
  - `The_pause_overlay_calls_resume_and_home`
  - `Game_over_has_home_and_a_hidden_revive_slot`
  - `The_hud_pause_button_calls_pause`
  - `A_safe_area_panel_matches_the_safe_area_anchors`: compare it with `SafeArea.Anchors` for the current screen.
- [ ] **Step 2: See them fail; implement; see them pass** (EditMode).
- [ ] **Step 3: Commit:** "Add the safe-area panel, the HUD pause button, the pause overlay and Home on game over".

---

### Task 4: The game screen

**Files:**
- Modify: `GameController.cs`
- Test: `Tests/PlayMode/GameFlowTests.cs`. Its existing tests must stay green, and new tests are added.

**Interfaces:**
- Consumes: Tasks 1–3.
- Produces on `GameController`:
  - `event Action? HomeRequested`: raised by Home on the pause overlay and on game over.
  - `void Boot(SaveStore store, SaveData save)`. App calls this so both share one SaveData. `Boot(string savePath)` stays, building its own store and data.
  - `PauseView Pause`, `void TogglePause()` (the back button), `bool IsPaused`.
  - It cancels a drag in `OnDisable`, and when pausing.
  - It builds its UI under a `SafeAreaPanel`. It passes the banner height into `BoardLayout`, and the score band's screen fractions (from `ScoreBandTop` and `BoardTopLeft`) into `HudView`:
    - `BannerSlot.HeightPixels(Screen.dpi, Screen.width)`, converted to world units as a share of `Screen.height`.
    - With no screen, it reserves nothing.
  - While paused, `Press` is refused, by checking `IsPaused` before calling `Drag.Press`. The board stays visible behind the overlay.

- [ ] **Step 1: Write the failing PlayMode tests:**
  - `Pause_stops_dragging_and_resume_continues`
  - `Pause_home_and_game_over_home_raise_HomeRequested`
  - `Hiding_the_game_mid_drag_sends_the_piece_back`: deactivate the object, then reactivate it.
  - `The_banner_band_is_kept_clear`: the board's top-left is below the world band reserved at the top.
- [ ] **Step 2: See them fail; implement; run PlayMode, then EditMode.** Expected: all green.
- [ ] **Step 3: Commit:** "Pause, Home requests and a banner-aware layout on the game screen".

---

### Task 5: App, Home and Settings

**Files:**
- Create: `App.cs`, `UI/HomeScreen.cs`, `UI/SettingsScreen.cs`
- Modify: `EditorTools/ProjectSetup.cs` (`BuildScene` puts an `App` object in the scene), then regenerate `Scenes/Game.unity`
- Test: `Tests/PlayMode/AppFlowTests.cs`

**Interfaces:**
- Produces `sealed class App : MonoBehaviour`:
  - `void Boot(string folder)`: the save and settings files live in `folder`. `Start` calls `Boot(Application.persistentDataPath)` unless a test already called it.
  - `HomeScreen Home`, `SettingsScreen Settings`, `GameController Game`. Game is created the first time Play is pressed.
  - `AppScreen Current`, where `enum AppScreen { Home, Game, Settings }`.
  - `void ShowHome()`, `void PlayOrContinue()`, `void ShowSettings()`, `void Back()`. `Update` calls `Back` when `Keyboard.current.escapeKey.wasPressedThisFrame`, which is the Android back button.
  - Showing a screen deactivates the others' root objects. The game keeps its state while hidden.
- Produces `HomeScreen`:
  - `static HomeScreen Create(Transform parent, Action play, Action settings)`.
  - `void Refresh(bool gameInProgress)`: sets the main label to "Continue" or "Play".
  - `string PlayLabel`, `bool DailyEnabled` (always false here), `PressPlay()`, `PressSettings()`.
- Produces `SettingsScreen`:
  - `static SettingsScreen Create(Transform parent, SettingsData settings, Action changed, Action back)`.
  - Four toggle rows, and the version text `"Version " + Application.version`.
  - `void SetToggle(string name, bool on)` for tests, with names "Sound", "Music", "Haptics" and "Reduced motion".
  - `PressBack()`.

- [ ] **Step 1: Write the failing PlayMode tests.** Each one boots an `App` with its own temp folder and camera.
  - `Booting_shows_home_with_play`
  - `Play_starts_a_game_and_continue_resumes_it_after_home`: place a piece, Pause, Home. The label reads "Continue". Continue, and the Placements and Board are unchanged.
  - `After_game_over_home_offers_play_not_continue`: save the one-move-from-game-over position, Continue, place `"i2h"` at (0, 1), then Home. The label reads "Play".
  - `Settings_are_remembered_across_boots`
  - `Back_goes_from_settings_to_home_and_pauses_a_game`
  - `Daily_is_shown_but_disabled`
- [ ] **Step 2: See them fail; implement.** Then:
  - Run `ProjectSetup.BuildScene`.
  - Run PlayMode and EditMode. Expected: all green.
  - Open the scene in the Editor and press Play: Home → Play → drag → Pause → Home → Settings.
- [ ] **Step 3: Commit:** "Add the app shell: Home, Game and Settings with Continue and the back button".

---

### Task 6: Docs, build and results

- [ ] **Step 1: Docs.**
  - README: the scene's root is now `App`.
  - This plan's Results: test counts, and every decision made while building.
  - The [Task 1.8 plan](2026-10-20-task-1.8-unity-prototype.md): its "HUD ignores the safe area" note points here.
- [ ] **Step 2: Build the development APK.** It must still build, and stay under 80 MB.
- [ ] **Step 3: Run all three suites:**
  - `dotnet test core/BlockPuzzle.slnx -c Release`
  - EditMode
  - PlayMode
- [ ] **Step 4: Commit:** "Document the app shell".

## Not in this plan

- **Daily Puzzle** (task 2.5): Home only shows its disabled button.
- **Feedback, sounds and haptics** (task 2.2): the toggles are stored and do nothing yet.
- **Art** (task 2.1): still plain squares and built-in text.
- **Translations** (task 2.8): UI strings are English literals for now. Task 2.8 moves them into Unity Localization tables.
- **Phase 3 rows:** Privacy Settings, Restore Purchases, the revive offer, and the banner ad itself. Its slot is reserved.
- **Support and Privacy Policy links:** they need task 0.5's website.

## Results

Tasks 1–6 were built on 2026-10-20, test-first.

- **Tests:** 82 EditMode and 33 PlayMode tests pass, and the 161 .NET tests are unchanged.
- **Screens:**
  - The scene holds a camera, a 2D light and `App`, which builds Home, the game and Settings.
  - Home's main button reads Continue or Play; Daily shows "Daily · soon", disabled.
  - The game has a pause button, a pause overlay (Resume, Home) and game over with Home and a hidden revive slot.
  - Settings stores four toggles at once on every change, and shows the version.
- **Layout:** a banner band of 50 dp on phones and 90 dp on tablets is kept free at the top of the safe area. At 9:16, 9:21 and 3:4 the score, board and tray still fit.
- **Android:** the development APK still builds: 64 MB, under the 80 MB download limit.

Decisions made while building:
- **The HUD's anchors come from the camera view,** under the full-screen canvas rather than the safe-area panel. The layout's score band is already inside the safe area and below the banner band, so a second inset would count the notch twice.
- **What sits where:**
  - The pause and game-over overlays are full-screen dims with their buttons in the middle.
  - The development overlay sits in the safe-area panel, with its Bench button in the banner band, which stays empty until Phase 3.
- **Pausing turns `PointerInput` off,** and `PointerInput.Apply` ignores frames while it's off. That refuses presses, as planned, and also drags and releases. The board stays visible behind the overlay.
- **`SafeAreaPanel` applies its anchors in `Create` instead of `Awake`,** because `Awake` doesn't run in EditMode tests. `Update` passes the live values to `ApplyIfChanged`, which reapplies them only when the safe area or the screen size changes.
- **Shared code:**
  - `UiFactory` gained `Canvas` and `EnsureEventSystem`, moved out of `GameController`, so App and the screens share them.
  - App creates the EventSystem before any screen, so a hidden game never takes it down with it.
  - `SaveStore.FileName` and `SettingsStore.FileName` name the two files.
- **Test helpers:** `PointerFor`, `DragAndRelease`, the one-move-from-game-over board and saving a position moved to `Tests/Shared`, for `GameFlowTests` and `AppFlowTests`.
- **Home's title** is `Application.productName` until task 0.2 names the game.
- **The back button on game over** does nothing; the panel has Home and Play again.
- **Task 1.8's phone checks** moved to the master plan's tasks 4.1 and 4.2, so they no longer hold anything up.

A fresh review of the branch found no runtime defects. Its findings were fixed test-first:
- **Test gaps closed:**
  - Pausing mid-drag sends the piece back, and the next touch after Resume starts a fresh drag. A version without the cancel was checked and fails this test.
  - The safe-area panel moves when the safe area changes. In the Editor the safe area is the whole screen, so the old test couldn't fail.
- **Home's label:** before the game screen exists, it asks `GameController.CanResume`, so a saved game that is already over, or can't be resumed, offers Play, not Continue. Play then starts a new game instead of showing an old game over.
- **More tests:**
  - A damaged settings file still boots, with the saved game intact.
  - The Escape key (Android's back button) goes back.
  - Back does nothing on game over.
  - Each settings toggle changes only its own setting.
