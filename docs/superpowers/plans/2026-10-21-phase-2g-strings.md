# Phase 2g: Every UI String in One Table (Translations, Step 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The first step of master-plan task 2.8. Every word the player reads comes from one keyed table, `Strings`, so adding Brazilian Portuguese, Latin-American Spanish and Indonesian changes one file and no screens.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), task 2.8: English, Brazilian Portuguese, Latin-American Spanish and Indonesian.

## Global Constraints

- **Keys never change; texts may.** Settings toggles are identified by ids ("sound", "music", "haptics", "reduced_motion"), never by their English labels.
- **Numbers are written with the invariant culture.**
- **A missing key shows itself,** so a gap is visible on screen and never blank.
- **What stays as it is:**
  - The development overlay's text.
  - The pause glyph "II".
  - The game's name, which comes from `Application.productName`.

## Tasks

- [ ] **Tests first** (`Tests/EditMode/StringsTests.cs`):
  - every key has English text;
  - a missing key shows itself;
  - numbers are written the same way everywhere.
- [ ] **Move the literals:**
  - Home, Pause, Game over (both titles), the HUD, praise, the tutorial's hints and Skip, Settings and the share text all read from `Strings`.
  - `SettingsScreen.IsOn` and `SetToggle` take ids.
- [ ] **Run the suites.** Every existing test still sees the same English. Commit.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 121 EditMode and 52 PlayMode tests pass. The English on screen is unchanged, so every existing text assertion still holds.
- **The table:** `Strings` holds 37 keys.
- **Toggle ids:** the Settings toggles moved from labels to ids.
- **Step 2 (the three languages):**
  - It adds a dictionary per language behind the same keys, and picks one from the phone's language.
  - Its open question is whether to move the table into Unity's Localization package, which the master plan names. That package brings Addressables into the build. A code table is simpler for about 40 strings. The keys carry over either way.
