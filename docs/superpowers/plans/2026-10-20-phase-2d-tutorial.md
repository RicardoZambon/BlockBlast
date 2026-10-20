# Phase 2d: First-Time Tutorial Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A new player's first Play is three guided moves on a preset board, with a moving hand and one short line of text, then straight into Classic. Returning players never see it.

**Architecture:**
- **`TutorialMode`** is a `GameMode`. Its three steps share one board. Each step's single piece may land only on its target, so the board at each step is exactly what the player just made:
  1. A 2×2 square anywhere.
  2. A three-cell bar that completes row 7.
  3. A three-cell bar that completes column 7.
- **Guides:**
  - `GameMode.CurrentGuide` names the slot, the target and the hint.
  - `SnapFinder` and `DragSession` accept only that target while a guide is set. A drop anywhere else sends the piece back.
- **`GameController`:**
  - It shows the hint and the hand, and replaces the game with the next step after a guided move.
  - It raises `Completed` after the last step, or on Skip.
  - App then marks the tutorial done and opens Classic.

**Tech Stack:** as in Phase 2c.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), task 2.4: "Two or three guided moves on a preset board, with a hand pointer and almost no text. Returning players skip it."

## Global Constraints

- The earlier plans' Global Constraints still hold.
- **Who sees it:** the first Play on a save with no progress. That means no `TutorialDone`, no Classic game in progress or played, no best score, and no Daily played or in progress.
  - A save from before this plan with any progress skips it.
  - The Daily never starts it.
- **Almost no text:** one line per step, plus a Skip button.
- **Nothing is scored or saved mid-tutorial.** Leaving through pause and Home starts it over next time.

## Review Focus

- **A drop off the target:** the piece goes back and the step doesn't advance. *Task 1:* the guide test. *Task 2:* the PlayMode drop test.
- **A returning player:** any progress skips the tutorial. *Task 1:* the `ShouldShow` tests.
- **The step after a clear:** the engine deals a fresh tray when the single piece is placed. The next step's game replaces it before anything renders, so those pieces never show. *Task 2.*
- **Skip and Home mid-tutorial:** Skip marks it done and opens Classic. Home leaves it not done. *Task 2.*
- **Dragging stays allocation-free** with a guide set, or none. *Task 1:* the existing allocation test still passes.

---

### Task 1: Guided moves and the tutorial's steps

**Files:**
- Create: `unity/Assets/Game/TutorialMode.cs`
- Modify: `GameMode.cs` (the `Guide` struct, plus `CurrentGuide`, `NextStep`, `IsComplete` and `Skip`), `Presentation/SnapFinder.cs`, `Presentation/DragSession.cs`, `Presentation/DragController.cs` (`SetGuide`), `Presentation/BoardLayout.cs` (`PointerFor`), `Services/SaveData.cs` (`TutorialDone`)
- Test: `Tests/EditMode/TutorialTests.cs`. `SnapFinderTests` and `DragSessionTests` stay green.

**Interfaces:**
- `readonly struct Guide(int slot, int row, int col, string hint)`.
- `SnapFinder.TryFind(..., int onlyRow = -1, int onlyCol = -1)`: with a target set, only that anchor counts.
- `DragSession.SetGuide(int row, int col)`; `-1, -1` clears it.
- `BoardLayout.PointerFor(int row, int col, int width, int height)`: where a finger goes so the lifted piece's top-left lands on (row, col). The tests' `TestDrags.PointerFor` uses it.
- `TutorialMode(SaveData save)`:
  - `static bool ShouldShow(SaveData save)`.
  - `NextStep` returns the next step's game on the current board, or null after the last step, which sets `TutorialDone`.
  - `Skip()` sets `TutorialDone`.

- [ ] **Step 1: Write the failing EditMode tests:**
  - `Each_guided_move_is_legal_and_the_bars_clear_a_line`
  - `The_guide_lets_the_piece_land_only_on_its_target`
  - `Skip_marks_the_tutorial_done`
  - `Only_a_save_without_progress_shows_the_tutorial`
- [ ] **Step 2: See them fail; implement; run EditMode.**
- [ ] **Step 3: Commit:** "Add guided moves and the tutorial's steps".

---

### Task 2: The tutorial in the app

**Files:**
- Create: `unity/Assets/Game/UI/TutorialHintView.cs` (the line of text and Skip), `unity/Assets/Game/Presentation/HandMarker.cs` (the moving hand)
- Modify: `GameController.cs` (guides, `NextStep`, `Completed`), `App.cs` (`AppScreen.Tutorial`, and the first Play), `Presentation/CellSprite.cs` (a circle for the hand)
- Test: `Tests/PlayMode/AppFlowTests.cs`. Its tests that press Play on a new save first mark the tutorial done.

- [ ] **Step 1: Write the failing PlayMode tests:**
  - `The_first_play_runs_the_tutorial_then_classic`
  - `A_drop_off_the_guide_is_refused`
  - `Skip_goes_straight_to_classic`
  - `A_returning_player_skips_the_tutorial`
  - `Home_mid_tutorial_starts_it_over_next_time`
- [ ] **Step 2: See them fail; implement; run EditMode and PlayMode.**
- [ ] **Step 3: Commit:** "Run the tutorial on a new player's first Play".

---

### Task 3: Docs and checks

- [ ] This plan's Results, and the README's plan list.
- [ ] Build the development APK. Run all three suites.
- [ ] **Commit:** "Document the tutorial".

## Not in this plan

- **Art for the hand:** a white circle until task 2.1.
- **Translations of the hint lines:** task 2.8.

## Results

Tasks 1–3 were built on 2026-10-20, test-first.

- **Tests:** 96 EditMode and 48 PlayMode tests pass. The 173 .NET tests are unchanged, because the engine wasn't touched.
- **The tutorial:**
  - A new player's first Play runs three guided moves on one board: a 2×2 square, then a bar that clears row 7, then one that clears column 7. Classic starts right after.
  - Each step shows one line of text and a disc that slides from the piece to where the finger goes.
  - Skip ends it at once, and Home starts it over next time.
  - A save with any Classic or Daily progress never shows it.
- **Guides:** while a guide is set, a piece lands only on its target, and a drop anywhere else sends it back. A build without the guide was checked and fails `A_drop_off_the_guide_is_refused`.

Decisions made while building:
- **Only the target accepts the piece,** so every step continues on the board the player made, without fallbacks for a misplaced piece.
- **The next step's game replaces the engine's freshly dealt tray** in `OnPlaced`, before anything renders it.
- **No tutorial state is saved.** `TutorialMode.InProgress` does nothing. The file is rewritten, unchanged, when the tutorial starts, and again with `TutorialDone` when it ends.
- **Button labels:** their padding is now a share of the button's height, so the 100-tall Skip keeps a readable label. The 170- and 200-tall buttons look as before.
- **The tests' `PointerFor`** now uses `BoardLayout.PointerFor`, which the hand needs too.

A fresh review found no critical or important defects. Its minor findings were fixed:
- **Tests that couldn't fail:**
  - The off-guide drop test now checks that the board and the piece stay put. Before, it only checked counts that a step change resets anyway.
  - `TutorialDone` is checked on a tutorial screen without App, so nothing else writes the save after Skip.
- **New tests:** a guided drag allocates nothing, and the back button pauses the tutorial.
- **Left for Phase 3:**
  - The hint and Skip use the banner band, so the banner must stay hidden during the tutorial.
  - In development builds the hint's box overlaps the Bench button's edge; the centred text clears it.
