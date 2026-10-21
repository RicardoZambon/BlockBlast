# Phase 4b: Store Listing Drafts (4.3, Text)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The text of master-plan task 4.3's store listings in English, Brazilian Portuguese, Latin-American Spanish and Indonesian. It's written as fastlane metadata, so the upload lanes (4.6) send it as it is. The name waits for you (0.2); the icon, video and screenshots wait for the art (2.1).

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 4.3, the Global Constraints (identity, 13+), and the [design doc](../../design.md).

## Rules

- **Only what's built:**
  - the 8×8 board and three pieces;
  - Classic and the Daily Puzzle, with its one try, streak and Share;
  - snapping with the line preview;
  - sound and haptics with off switches, and reduced motion;
  - offline and no account;
  - a save after every move;
  - four languages;
  - Remove Ads.
- **The game's own words:** each language uses its in-game terms: Desafio diário, Reto diario, Tantangan harian, and the Remove Ads labels.
- **Genre words only** in keywords, never another game's name or trademark. The check refuses "blast", "tetris" and other well-known names.
- **Limits:**
  - **App Store:** name 30, subtitle 30, promotional text 170, description 4000, and keywords 100 bytes (Apple counts bytes there, so accented letters cost two).
  - **Google Play:** title 30, short description 80, full description 4000.
- **Locales:**
  - App Store: `en-US`, `pt-BR`, `es-MX` (its Latin-American Spanish) and `id`.
  - Google Play: `en-US`, `pt-BR`, `es-419` and `id`.
- **Placeholders:** `[Game name]` for the name, and `[Domain]` in the App Store support and privacy URLs.

## Tasks

- [x] `fastlane/metadata/<locale>/` (App Store) and `fastlane/metadata/android/<locale>/` (Google Play), in four languages.
- [x] `fastlane/check-metadata.sh` and the `listing` workflow: the limits and the forbidden names on every change.

## Results

Written on 2026-10-21. Every field passes the check:
- **Tightest fields:** the subtitles use 27–29 of 30 characters, and the keywords 85–99 of 100 bytes.
- **Roomiest:** the descriptions are about 1,000–1,150 of 4,000 characters, which leaves room for the name and later features.

Still yours:
- **The name (0.2):** it replaces `[Game name]` in each `name.txt`, `title.txt` and description, and must fit 30 characters.
- **The domain (0.5):** it replaces `[Domain]`.
- **A native speaker's read** of each translation, as for the game's strings (2.8).
- **The pictures:** the icon, the 15–30 s preview video and the screenshots for each device size come with the art (2.1). Claude can capture the screenshots on the emulator once the art is in.
