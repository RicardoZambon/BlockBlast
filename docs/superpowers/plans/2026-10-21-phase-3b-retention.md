# Phase 3b: Retention Helpers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Master-plan task 3.7:
- a rating prompt after a new high score;
- a Daily reminder, asked for only after a player's first Daily.

**Architecture:**
- **`RetentionRules`** (pure, tested) decides:
  - when to ask for a rating;
  - when to ask for reminder permission;
  - when the next reminder fires.
- **`IReviewPrompt` and `IReminders`** hide the native APIs:
  - Android reminders are real, through Unity Mobile Notifications (2.5.0, from Unity's registry, no account needed).
  - The review prompt is a stand-in until Google's In-App Review plugin and Apple's StoreKit prompt are added. Google's plugin isn't in Unity's registry.
- **`GameController`** raises `GameRecorded` before it saves a finished game. App adds its prompt decisions to the same save.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 3.7.

## Global Constraints

- **Rating:**
  - Ask only after a new Classic high score.
  - Ask only once the player has finished `review_prompt_min_games` games: 5 until Remote Config (3.1).
  - Ask at most once every 60 days, even though the stores add their own limits.
- **Reminder:**
  - Ask for notification permission only after the first finished Daily, and only once.
  - "Today's puzzle is ready", at most once a day, at 18:00 local.
  - It fires only once a new, unplayed puzzle exists, in every time zone, so it never announces the puzzle the player just finished.
  - Each finished Daily schedules the next reminder, replacing the last one, so a missed day ends the reminders until the next Daily.
- **Text:** in every language (`notify.daily_ready`).

## Tasks

1. **`RetentionRules` with EditMode tests:**
   - the rating's conditions;
   - the one-time reminder ask;
   - the reminder's time in UTC−3, UTC+0 and UTC+7.
2. **Services, wiring and tests:**
   - Services: `IReviewPrompt`, `IReminders` and their stand-ins, plus `AndroidReminders`.
   - Save data: `RatingAskedDay`, `RemindersAsked` and `RemindersOn` in `SaveData`.
   - Wiring: App and `GameController.GameRecorded`.
   - PlayMode tests with fakes.
3. **Package, APK, docs and a PR.**

## Results

(Filled in at the end.)
