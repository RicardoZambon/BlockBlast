# Phase 2f: Accessibility Checks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The parts of master-plan task 2.9 that today's UI can meet, each made into a test so future art can't quietly break it:
- checked contrast;
- board states told apart by brightness as well as colour;
- a minimum text size.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), task 2.9: "pieces readable by brightness, not only by color; a minimum text size; checked contrast; an 'off' switch for every effect". The last item is done: Phase 2e's Sound, Haptics and Reduced motion toggles cover every effect.

## Global Constraints

- **Contrast**, using WCAG 2.1 ratios:
  - 4.5:1 for text against what's behind it, and for button labels.
  - 3:1 for a button against the background, and for a filled cell against an empty one.
- **Brightness:** every pair of board states (empty, filled, ghost, clearing) differs by at least 1.5:1, so they read without colour vision.
- **Minimum text size:** 36 units on the 1080×1920 reference canvas, about 12 sp on a phone. Best-fit text never shrinks below it. Development-only text (the debug overlay) is exempt.

## Tasks

- [ ] **Tests first** (`Tests/EditMode/AccessibilityTests.cs`):
  - `Text_on_the_background_meets_4_5_to_1`
  - `Button_labels_meet_4_5_to_1_and_buttons_stand_out`
  - `Board_states_differ_by_brightness_not_only_colour`
  - `Every_screen_keeps_its_text_at_or_above_the_minimum_size`
  - The PlayMode clear test also checks that the praise text has an outline.
- [ ] **Fix what fails:**
  - `Palette.Button`, a darker blue for buttons.
  - `Readability.MinTextSize` as the best-fit floor.
  - `UiFactory.Outlined` for the praise drawn over the board.
- [ ] **Docs; suites; commit.**

## Results

Built on 2026-10-20, test-first.

- **Tests:** 118 EditMode and 52 PlayMode tests pass, and the 173 .NET tests are unchanged.
- **What failed first:**
  - White labels on the old button blue (#4FA3F7) measured 2.65:1.
  - Best-fit text could shrink to 10 units.
- **What passed already:** text on the background, the streak's yellow, and all six pairs of board states, by brightness alone.
- **Fixes:**
  - Buttons use `Palette.Button` (#2A6BC9): 5.2:1 for their labels, and 3.05:1 against the background. Cells keep the brighter blue.
  - Best-fit text stops at `Readability.MinTextSize` (36).
  - The praise gets a dark outline, because white over a blue cell is only 2.65:1.
- **Left for later:**
  - Art (task 2.1) must keep these tests green. They read the palette, so new colours are checked automatically.
  - Large system font sizes are checked on phones in task 4.1.
