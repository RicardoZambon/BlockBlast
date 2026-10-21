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

Built on 2026-10-21, test-first.

- **Tests:** 143 EditMode and 64 PlayMode tests pass, and the 173 .NET tests are unchanged.
  - `RetentionRules` has 6 tests, including the reminder time in UTC−3, UTC+0 and UTC+7.
  - `RetentionFlowTests`, through App with fakes, covers:
    - a rating after a new best once 5 games are finished;
    - none without a new best;
    - the first Daily asking about reminders and scheduling one;
    - a later Daily rescheduling without asking again;
    - a refused permission never being asked again.
- **Android:**
  - Reminders are real, through Unity Mobile Notifications 2.5.0: one "daily" channel and at most one scheduled reminder.
  - Android 13 and later ask the player; older versions allow it.
  - The APK builds with them: 64 MB.
- **Stand-ins:**
  - The store review prompt waits for Google's In-App Review plugin and StoreKit's prompt.
  - iOS reminders wait for the iOS build (master plan 0.6).

Decisions made while building:
- **"New best"** compares the score with the best from before the game started, since the saved best rises during play. A game resumed after a kill doesn't count a best it had already set.
- **`GameRecorded` fires before the game's own save,** so the day a rating was asked, and that reminders were asked about, are written with the finished game. The reminder answer can come later, so App saves it itself.
- **The package's iOS default asked for permission at launch.** `NotificationsSettings.asset` now says no, and `ProjectSettingsTests` checks it, so permission is asked for only after the first Daily, as planned.
- **Reboots:** rescheduling after the phone restarts is off. It would add a boot permission; the next finished Daily schedules the next reminder anyway.
- **Reminders end after a missed day,** because each finished Daily schedules only the next one. A player who stops isn't nagged.
