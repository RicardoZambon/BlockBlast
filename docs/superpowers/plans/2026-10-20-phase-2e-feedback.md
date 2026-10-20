# Phase 2e: Visual and Sound Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every placement feels good: a squash when a piece lands, a sweep and particles when lines clear, praise for big clears, a streak counter, a light shake, haptics and sounds that rise with a streak. Each one obeys its Settings toggle.

**Architecture:**
- **`FeedbackRules.For(PlacementOutcome, SettingsData)`** is pure. It returns a `FeedbackCue`, which says what to play: the haptic level, the sound and its pitch, squash, sweep, particles, shake, a praise word and the streak.
- **`FeedbackPlayer`** plays a cue: board animations, pooled particles, the praise popup, the shake, Android haptics and procedural sounds.
  - `GameController` calls it after each free-play placement.
  - It shares App's `SettingsData`, so a toggle takes effect at once.
- **Sounds are generated in code** (`SoundBank`) until there are real assets. Music stays a stored toggle until there's music.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), task 2.2. The toggles exist from Phase 2a; task 2.9 checks contrast and text size later.

## Global Constraints

- The earlier plans' Global Constraints still hold. Dragging stays allocation-free; placement may allocate.
- **Haptics:**
  - Light when placing, medium when clearing, heavy when the whole board clears.
  - Nothing when Haptics is off.
  - Android only: `VibrationEffect` on API 26 and up, a plain vibrate below. Referencing `Handheld.Vibrate` makes Unity add the VIBRATE permission.
- **Reduced motion** turns off the squash, the particles and the shake. The sweep becomes a plain fade, and the praise a fade without scaling.
- **Sound off** plays nothing. Pitch rises 6% per streak step, up to 1.6×.
- **Praise:**
  - "Great!" at 2 lines, "Amazing!" at 3, "Incredible!" at 4 or more, "Board clear!" when the board empties.
  - A single-line clear in a streak of 3 or more gets "Streak N!".

## Review Focus

- **Toggles take effect at once:** turning off Sound, Haptics or Reduced motion in Settings changes the very next placement. *Task 1:* the rules. *Task 2:* App and the game share one `SettingsData`.
- **A shake never moves input:** only the board's visuals move, and they settle back exactly. *Task 2.*
- **Back-to-back clears:** a new clear while the last one still animates doesn't leave cells mid-fade. *Task 2.*

---

### Task 1: The rules

- **Files:** create `unity/Assets/Game/Presentation/FeedbackRules.cs` (`FeedbackCue`, `Haptic`, `Sound`); test `Tests/EditMode/FeedbackRulesTests.cs`.
- [ ] **Tests first:**
  - `A_placement_without_a_clear_squashes_and_taps_lightly`
  - `A_clear_sweeps_bursts_and_buzzes_medium`
  - `Big_clears_shake_and_get_praise`
  - `A_board_clear_is_heavy_with_its_own_praise`
  - `The_pitch_rises_with_the_streak_and_stops_at_1_6`
  - `A_long_streak_of_single_clears_gets_streak_praise`
  - `Reduced_motion_keeps_only_fades`
  - `Sound_and_haptics_off_play_nothing`
- [ ] Implement; run EditMode; commit "Decide each placement's feedback from its outcome and the settings".

### Task 2: Playing it

- **Files:**
  - Create: `Presentation/FeedbackPlayer.cs`, `Presentation/SoundBank.cs`, `Presentation/Haptics.cs`.
  - Modify: `Presentation/BoardView.cs` (`Squash`, `Sweep`), `UI/HudView.cs` (the streak), `GameController.cs` (a settings argument; play after placing), `App.cs` (pass `_settings`).
- **Tests:**
  - PlayMode `GameFlowTests`:
    - `A_line_clear_plays_the_sweep_and_praise_for_two_lines`
    - `Reduced_motion_plays_no_shake_or_particles`
    - `Sound_off_plays_no_sound`
    - `The_streak_shows_from_two`
  - EditMode `SoundBankTests.Every_sound_has_a_clip`.
- [ ] Tests first; implement; run EditMode and PlayMode; commit "Play feedback for every placement".

### Task 3: Docs and checks

- [ ] This plan's Results and the README's plan list.
- [ ] Build the APK; the Android haptics code only compiles there. Run all suites.
- [ ] Commit "Document the feedback".

## Not in this plan

- **Real sounds and music:** generated tones until assets are chosen (task 2.1, with you).
- **Art for particles and praise:** plain squares and the built-in font until task 2.1.

## Results

(Filled in by Task 3.)
