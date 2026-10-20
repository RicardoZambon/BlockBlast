# Phase 2b: Reliable Save and Resume Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A game survives anything a phone does to it: a call, switching apps, the notification shade, the OS killing the app, and a damaged save file.

**Architecture:**
- **Interruptions:** `GameController` pauses itself when the app goes to the background or loses focus, so the player comes back to the pause overlay instead of a running game.
- **Saves:** `JsonFile.Write` keeps the previous file as `<path>.bak`. `SaveStore` and `SettingsStore` fall back to it when the main file is missing, damaged or from another version, so the worst case loses one move instead of the game and the best score.

**Tech Stack:** the same as Phase 2a: Unity 6000.3.26f1, uGUI, Input System, Unity Test Framework.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), task 2.6. Builds on the [Phase 2a plan](2026-10-20-phase-2a-app-shell.md).

## Global Constraints

- Everything in the [Task 1.8 plan](2026-10-20-task-1.8-unity-prototype.md)'s and the [Phase 2a plan](2026-10-20-phase-2a-app-shell.md)'s Global Constraints still holds.
- **Master plan 2.6:** "Save after every move. Handle the app going to the background, incoming calls, the OS killing the app, and the phone's clock changing (which matters for the Daily)."
  - Saving after every move and resuming after a kill are done (Task 1.8). This plan adds the pause on interruption and the backup.
  - **The clock moves to task 2.5,** where the Daily needs it. Nothing in the game reads the clock yet.

## Review Focus

- **Killed between the two renames of a replace:** on Android, `File.Replace` with a backup renames the old file away before moving the new one in. A kill between the two leaves only the backup, which must be read. *Task 2:* the missing-main-file test.
- **An interruption on game over or while hidden:** the game must not pause behind the game-over panel, or while Home is showing. *Task 1:* the game-over test. A hidden object gets no messages.
- **Coming back from the background:** the game stays paused until Resume; nothing resumes on its own. *Task 1.*
- **A save from a newer version next to an older backup:** the backup is used only if its own version matches. *Task 2:* the version check runs on both files.
- **A read-only or locked save:** a failed write is still logged, not thrown, and the backup doesn't change that. *Task 2:* the existing failed-save tests stay green.

---

### Task 1: Pause when the app is interrupted

**Files:**
- Modify: `unity/Assets/Game/GameController.cs`
- Test: `unity/Assets/Game/Tests/PlayMode/GameFlowTests.cs`

**Interfaces:**
- `GameController` gains `OnApplicationPause(bool paused)` and `OnApplicationFocus(bool focused)`. Going to the background or losing focus pauses the game, unless it's already paused or game over is showing. Coming back does nothing.

- [ ] **Step 1: Write the failing PlayMode tests:**
  - `Going_to_the_background_pauses_the_game`: `SendMessage("OnApplicationPause", true)`. `IsPaused` and `Pause.IsShown` are true.
  - `Losing_focus_pauses_the_game`: `SendMessage("OnApplicationFocus", false)`.
  - `Coming_back_stays_paused`: pause by going to the background, then `OnApplicationPause(false)` and `OnApplicationFocus(true)`. Still paused.
  - `An_interruption_on_game_over_does_not_pause`: from the one-move-from-game-over position, place `"i2h"` at (0, 1), then go to the background. `IsPaused` is false and game over still shows.
- [ ] **Step 2: See them fail; implement; run PlayMode.** Expected: all green, including the existing mid-drag tests.
- [ ] **Step 3: Commit:** "Pause the game when the app is interrupted".

---

### Task 2: A backup save

**Files:**
- Modify: `unity/Assets/Game/Services/JsonFile.cs`, `SaveStore.cs`, `SettingsStore.cs`
- Test: `unity/Assets/Game/Tests/EditMode/SaveStoreTests.cs`, `SettingsStoreTests.cs`

**Interfaces:**
- `JsonFile`:
  - `static string BackupPath(string path)`: `path + ".bak"`.
  - `Write` replaces the old file with `File.Replace(temp, path, BackupPath(path))`, so the previous version becomes the backup. The first write has no backup.
  - `static bool TryReadOrBackup<T>(string path, Predicate<T> accept, out T data) where T : class, new()`: the main file if it reads and `accept` passes, otherwise the backup on the same terms, otherwise false.
- `SaveStore.TryRead` and `SettingsStore.Load` use `TryReadOrBackup` with their version checks.

- [ ] **Step 1: Write the failing EditMode tests:**
  - `SaveStoreTests`:
    - `A_damaged_save_falls_back_to_the_one_before`: write A, then B, then damage the main file. `TryRead` gives A.
    - `A_save_killed_between_renames_reads_the_backup`: write A, then B, then delete the main file. `TryRead` gives A.
    - `A_backup_from_another_version_is_ignored`: damage the main file, and give the backup another version. `TryRead` is false.
  - `SettingsStoreTests`: `A_damaged_settings_file_falls_back_to_the_settings_before`.
- [ ] **Step 2: See them fail; implement; run EditMode, then PlayMode.** Expected: all green, including the existing corrupt-file and failed-save tests.
- [ ] **Step 3: Commit:** "Keep the previous save as a backup and fall back to it".

---

### Task 3: Docs and checks

- [ ] **Step 1: Docs.**
  - This plan's Results.
  - The master plan's 2.6: the clock moves to 2.5.
- [ ] **Step 2: Run all three suites:** `dotnet test core/BlockPuzzle.slnx -c Release`, EditMode, PlayMode. Expected: all pass.
- [ ] **Step 3: Commit:** "Document reliable save and resume".

## Not in this plan

- **The clock and the Daily:** task 2.5.
- **Cloud saves:** not in the master plan. Saves stay on the phone.
- **Phone checks of interruptions** (a real call, the notification shade): task 4.1.

## Results

(Filled in by Task 3.)
