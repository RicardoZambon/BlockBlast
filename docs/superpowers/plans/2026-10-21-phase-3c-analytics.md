# Phase 3c: Analytics Events Behind Consent (3.1, App Side)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The master plan's analytics events, logged at the right moments with the right parameters, and none before the consent flow finishes. A stand-in sends nothing until Firebase is wired in with your project (3.1).

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 3.1's event table, and 3.2's consent rule.

## Events

| Event | Logged by | Parameters |
|---|---|---|
| `consent_result` | App, when consent finishes | `gdpr_applies`, `att_status` ("unknown" until UMP and ATT) |
| `tutorial_begin` / `tutorial_complete` | App | none |
| `game_start` | GameController, for a new Classic or Daily game | `mode`, `game_index`, `gen_profile` |
| `game_end` | GameController, when a finished game is recorded | `mode`, `score`, `placements`, `lines`, `max_streak`, `duration_s`, `revived` |
| `revive_offer` / `revive_accept` | GameController | `game_index` |
| `daily_complete` | App | `score`, `streak_days` |
| `share` | GameController | `content_type` = `daily_result` |

`ad_impression` waits for MAX (3.5). Firebase logs purchases by itself.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 143 EditMode and 69 PlayMode tests pass, and the 173 .NET tests are unchanged. `AnalyticsFlowTests` checks:
  - nothing before consent;
  - a Classic game's end and the next one's start;
  - the tutorial's begin and complete;
  - the Daily's end, completion and share;
  - a revive's offer and acceptance, with no `game_end` while the offer is up.
- **Consent gate:** `ConsentGatedAnalytics` drops every event until consent finishes. Firebase's consent mode decides the rest once it's wired in.
- **Wiring order:** App now wires a game screen before booting it, so the first `game_start` and a game over during boot are caught. Before, the ad service wasn't set during boot either.
- **Left for later:**
  - `game_end` on quitting mid-game: the master plan says "game over or quit". A quit isn't an end here, since Continue resumes the game.
  - `duration_s` of a resumed game counts from the resume.
