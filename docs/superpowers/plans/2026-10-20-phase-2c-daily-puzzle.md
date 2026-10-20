# Phase 2c: Daily Puzzle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One puzzle a day, the same for every player: one scored attempt, a day streak, and a Share button. Setting the phone's clock back can't replay a day.

**Architecture:**
- **Engine:** `DailyPuzzle` and `DailyRecord` in `core/src` hold the rules: the UTC day number, each day's seed, and the streak. They're pure C# with .NET tests and golden values, like the rest of the engine.
- **Game screen:**
  - A small `GameMode` decides where a game comes from and where it is saved. `ClassicMode` is today's behaviour; `DailyMode` deals today's seed into its own slot in the save.
  - `GameController` keeps everything else: the views, dragging, pause, and writing the file.
- **App:**
  - Home's Daily button reads Daily, Continue or Done.
  - A Daily screen is built each time Daily is pressed, and destroyed when the player leaves it. The attempt in progress is saved after every move, so it resumes.
  - The clock and the share action are passed into `App.Boot`, so tests can set the date and capture the shared text.

**Tech Stack:** as in Phase 2a. The engine also builds for .NET (`core/BlockPuzzle.slnx`).

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), task 2.5, including the clock item it took from 2.6. Builds on the [Phase 2a](2026-10-20-phase-2a-app-shell.md) and [Phase 2b](2026-10-20-phase-2b-save-and-resume.md) plans.

## Global Constraints

- Everything in the earlier plans' Global Constraints still holds: C# 9, `#nullable enable`, no `System.Random`, the engine as the only source of rules, golden values for anything seeded.
- **Master plan 2.5:** "the seed comes from the UTC date; one scored attempt per day; a day-streak counter stored on the phone; a share button that posts a short text result: score, streak and a link". Plus, from 2.6: "setting the clock back must not replay a day that was already scored".
- **The same puzzle for everyone:** a Daily game uses `GeneratorSettings.Baseline()`, game index 0 and no quick losses, so mercy never changes it.
- **The link** in the shared text waits for a store page or the website (tasks 0.5 and 4.3). Until then the text has no link, and one constant adds it later.
- **Saves stay compatible:** an older save without Daily data loads with an empty Daily. `SaveData.CurrentVersion` stays 1.

## Review Focus

- **Clock set back after a Daily:** Home offers no new attempt, and pressing Daily does nothing. *Task 1:* `CanStart`. *Task 3:* the clock-back test.
- **An attempt left unfinished over midnight UTC:** opening the Daily on a later day drops it and deals the new day's puzzle. It never scores for the wrong day. *Task 3.*
- **Classic and Daily together:** each keeps its own game in progress, and neither changes the other's best score or game index. *Task 3.*
- **Old saves:** a save written before this plan loads, with an empty Daily. *Task 2.*
- **Determinism:** a day's first tray is a golden value in .NET and again in Unity, so every device deals the same puzzle. *Tasks 1 and 2.*

---

### Task 1: The Daily rules in the engine

**Files:**
- Create: `core/src/DailyPuzzle.cs`, `core/src/DailyRecord.cs`, and their `.meta` files once Unity imports them
- Test: `core/dotnet/BlockPuzzle.Core.Tests/DailyPuzzleTests.cs`

**Interfaces:**
- `static class DailyPuzzle`:
  - `static int DayNumber(DateTime time)`: whole UTC days since 1970-01-01. A local time is converted to UTC, and an unspecified one counts as UTC. Days before 1970 round down.
  - `static ulong Seed(int day)`: SplitMix64 of a fixed salt plus the day times the golden-ratio constant.
  - `static DateTime DateOf(int day)`: the UTC midnight that starts the day.
  - `static GameSession NewGame(int day)`: Standard catalog, `Baseline()`, default scoring, `Seed(day)`, game index 0, no quick losses.
- `[Serializable] sealed class DailyRecord` with public fields `LastDay = -1`, `LastScore`, `Streak`, `BestStreak` and `BestScore`:
  - `bool CanStart(int today)`: `today > LastDay`.
  - `int StreakOn(int today)`: `Streak` while `today <= LastDay + 1`, otherwise 0.
  - `void Finish(int day, int score)`: ignored unless `day > LastDay`. Otherwise it sets `Streak` to `Streak + 1` if `day == LastDay + 1`, or 1 if not. It keeps the bests and sets `LastDay` and `LastScore`.

- [ ] **Step 1: Write the failing xUnit tests:**
  - `Day_numbers_count_whole_utc_days_from_1970`: 1970-01-01 00:00 is 0; 2026-10-20 23:59:59 and 2026-10-21 00:00 are a day apart; a local time gives the same day as its UTC time.
  - `Days_before_1970_round_down`: 1969-12-31 23:00 UTC is −1.
  - `Each_day_has_its_own_seed_and_it_never_changes`: the golden `Seed(day)` for 2026-10-20; neighbouring days differ.
  - `A_daily_game_deals_the_same_first_tray_everywhere`: the golden tray of `NewGame(day)` for 2026-10-20.
  - `DailyRecord`:
    - `The_first_daily_starts_a_one_day_streak`
    - `The_next_day_extends_the_streak`
    - `A_missed_day_restarts_the_streak`
    - `A_day_scores_only_once`
    - `Setting_the_clock_back_cannot_start_or_score_an_earlier_day`
    - `The_streak_lapses_after_a_missed_day`
    - `The_bests_keep_the_highest`
- [ ] **Step 2: See them fail; implement; run `dotnet test core/BlockPuzzle.slnx -c Release`.** Fill in the golden values from the first passing run, and check that a different salt breaks them.
- [ ] **Step 3: Commit:** "Add the Daily Puzzle's rules to the engine".

---

### Task 2: Game modes and the Daily save slot

**Files:**
- Create: `unity/Assets/Game/GameMode.cs` (`GameMode`, `ClassicMode`, `DailyMode`), `unity/Assets/Game/Services/DailySave.cs`
- Modify: `SaveData.cs`, `SaveStore.cs`, `GameController.cs`, `UI/GameOverView.cs`
- Test: `Tests/EditMode/SaveStoreTests.cs`, `EngineGoldenTests.cs`, `ViewTests.cs`. `Tests/PlayMode/GameFlowTests.cs` must stay green.

**Interfaces:**
- `[Serializable] sealed class DailySave`: `bool HasGame`, `int GameDay = -1`, `GameSnapshot Game`, `DailyRecord Record`. `SaveData.Daily` holds one.
- `abstract class GameMode`:
  - `GameSession? Resume()` and `GameSession StartNew()`.
  - `void InProgress(GameSession game)` and `void Finished(GameSession game)` record into the save data. `GameController` writes the file.
  - `int Best`, `int Streak`, `string Title`, `bool CanPlayAgain` and `string? ShareText(GameSession finished)`.
- `ClassicMode(SaveData save)`: what `GameController` does today, including clamping damaged counters. `static bool CanContinue(SaveData save)` replaces `GameController.CanResume`.
- `DailyMode(SaveData save, int today, string product)`:
  - Resumes only an attempt from `today`, and drops any other.
  - Starts with `DailyPuzzle.NewGame(today)`, and finishes into `Record.Finish(GameDay, score)`.
  - The title is "Daily done". It offers no Play again. Its share text comes from `DailyShare.Text`.
  - `static DailyState StateOn(SaveData save, int today)` returns `Start`, `Continue` or `Done`.
- `GameController.Boot(SaveStore store, SaveData save, GameMode mode, Action<string>? share = null)`. The two existing `Boot` overloads use `ClassicMode`.
- `GameOverView.Create(Transform canvas, string title, Action? playAgain, Action? share, Action home)` and `Show(int score, int best, int streak)`: a streak line appears when the streak is above 0. `PressShare()`.

- [ ] **Step 1: Write the failing tests:**
  - `An_old_save_without_daily_data_loads_with_an_empty_daily`: write JSON without `Daily`. `Daily.Record.LastDay` is −1 and `Daily.HasGame` is false.
  - `EngineGoldenTests.A_daily_deals_the_same_first_tray_as_dotnet`, using the Task 1 golden value.
  - `ViewTests.A_daily_game_over_offers_share_and_no_play_again`.
- [ ] **Step 2: See them fail; implement; run EditMode and PlayMode.** Expected: all green.
- [ ] **Step 3: Commit:** "Add game modes, with the Daily's own save slot".

---

### Task 3: The Daily in the app

**Files:**
- Create: `unity/Assets/Game/UI/DailyShare.cs` (the text, and `Share(string)`: an Android share sheet, the clipboard elsewhere)
- Modify: `App.cs`, `UI/HomeScreen.cs`, `UI/UiFactory.cs` (button labels shrink to fit)
- Test: `Tests/PlayMode/AppFlowTests.cs`, `Tests/EditMode/DailyShareTests.cs`

**Interfaces:**
- `App.Boot(string folder, Func<DateTime>? utcNow = null, Action<string>? share = null)`. `AppScreen` gains `Daily`. `App.Daily` is the Daily's `GameController`, or null. `App.PlayDaily()` does nothing when the state is `Done`.
- `HomeScreen.Refresh(bool gameInProgress, DailyState daily, int streak)`. The Daily label reads:
  - "Daily", or "Daily · N-day streak" when there's a streak.
  - "Daily · continue" while an attempt is in progress.
  - "Daily · done" when it's played; the button is then disabled.
- `DailyShare.Text(string product, int day, int score, int streak)`: "Block Puzzle Daily 2026-10-20: 1240 points, 3-day streak". There's no streak part at 0, and the link is appended once `DailyShare.Link` is set.

- [ ] **Step 1: Write the failing tests** (each boots `App` with a fixed clock and a share capture):
  - `Daily_deals_todays_puzzle_and_home_offers_to_continue_it`
  - `Finishing_the_daily_extends_the_streak_and_offers_share`
  - `A_played_daily_waits_for_the_next_day`
  - `Setting_the_clock_back_cannot_replay_a_day`
  - `An_attempt_left_from_yesterday_is_dropped`
  - `Classic_and_daily_keep_separate_games`
  - `DailyShareTests`: the text, with and without a streak.
  - Replace `Daily_is_shown_but_disabled`.
- [ ] **Step 2: See them fail; implement; run EditMode and PlayMode.** Expected: all green.
- [ ] **Step 3: Commit:** "Add the Daily Puzzle to the app".

---

### Task 4: Docs, build and checks

- [ ] **Step 1: Docs.** This plan's Results, and the README's plan list.
- [ ] **Step 2: Build the development APK.** The Android share code only compiles there, so this checks it builds.
- [ ] **Step 3: Run all three suites.** Expected: all pass.
- [ ] **Step 4: Commit:** "Document the Daily Puzzle".

## Not in this plan

- **The link in the shared text:** tasks 0.5 and 4.3.
- **A Daily reminder notification:** task 3.7 (Retention helpers).
- **Sharing on iOS:** it needs a native share sheet. Builds other than Android copy the text to the clipboard.
- **Translations:** task 2.8.

## Results

(Filled in by Task 4.)
