# Phase 3a: Ad Placements Behind a Consent Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The app side of master-plan tasks 3.4 (where ads appear) and 3.2 (consent before any ad), built and tested now with stand-in services:
- an interstitial between games;
- a rewarded revive on the game-over screen;
- the banner in its reserved slot.

The real AppLovin MAX and Google UMP plugins (3.3) replace the stand-ins later, behind the same interfaces, once your accounts exist.

**Architecture:**
- **`IConsentFlow`** asks for consent once at launch. **`IAdService`** wraps an ad SDK.
- App starts the ad service only after consent finishes. Before that, nothing loads: the master plan's "no ad or analytics SDK collects data before the consent flow finishes".
- **Stand-ins:**
  - `StandInConsent` finishes at once.
  - `StandInAds` has ads ready at once and shows them as no-ops. Tests use controllable fakes.
- **Placements:**
  - **Interstitial:** after a game over, Play again and Home first ask the engine's `AdPolicy`, which counts grace games and the cooldown. A ready interstitial shows, and the action continues when it closes. Never during a game.
  - **Rewarded revive:** the game-over panel's revive slot shows "Revive" when a rewarded ad is ready and the game hasn't been revived. A completed ad calls `GameSession.TryRevive`, and the game goes on. Classic only, because the Daily is one fair attempt.
  - **Banner:** shown on the Classic and Daily screens, and hidden everywhere else, including the tutorial.
- **`RetrySchedule`** (pure): the waits for a failed load, 2, 4, 8… up to 64 s, for the MAX wrapper to use.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 3.2 and 3.4. The engine's `AdPolicy` and `TryRevive` are already tested.

## Global Constraints

- The earlier plans' Global Constraints still hold.
- **Consent first:** `IAdService.Initialize` is never called before `IConsentFlow` reports it has finished.
- **Interstitials:** only between games, only when `AdPolicy` allows, never with Remove Ads (always false until 3.6), and never in the tutorial.
- **Revive:** once per game, never offered without a loaded ad, and a skipped or failed ad gives nothing.
- **Remote Config later (3.1):** the policy settings use `AdPolicySettings` defaults: 3 grace games and a 90 s cooldown.

## Review Focus

- **A slow consent flow:** no ad loads, and Play still works.
- **The cooldown:** two quick game overs show one interstitial.
- **A revived game that ends again** offers no second revive.
- **An interstitial that fails to show:** the action still continues.

## Tasks

1. **Pure parts:** `RetrySchedule`, with EditMode tests.
2. **Services and the gate:**
   - `IConsentFlow` and `IAdService`, with their stand-ins.
   - `App.Boot` takes both, and starts ads after consent.
   - PlayMode tests with fakes.
3. **Placements:**
   - The interstitial in `App`, for Play again and Home after a game over.
   - The revive button in `GameOverView`.
   - The banner by screen.
   - PlayMode tests.
4. **Docs, APK, suites, PR.**

## Results

Built on 2026-10-21, test-first.

- **Tests:** 136 EditMode and 59 PlayMode tests pass, and the 173 .NET tests are unchanged.
  - `RetrySchedule` has 8 cases.
  - `AdFlowTests` covers:
    - consent first, and a game that doesn't wait for it;
    - an interstitial after the grace games, then the cooldown;
    - none during the grace games;
    - a revive that continues the game once, and none for a game already revived;
    - no revive without a loaded ad, and nothing from a skipped one;
    - the banner on game screens only.
- **The revive and the save:**
  - When game over offers a revive, the game isn't recorded as finished until the player leaves game over.
  - Killed on that panel, the app comes back to it. Home reads Play, because the game is over, and the revive is still there.
  - Without an offer, nothing changes: the game is recorded at once, as before.

Decisions made while building:
- **A first run failed four ad tests** because NUnit reuses one test-class instance, and the fakes carried state between tests. Each test now builds fresh fakes.
- **The cooldown uses the same clock as the Daily,** so tests can move it.
- **Rewarded ads count toward the cooldown,** like interstitials, because both are full screen.
- **Leaving game over records the game before the interstitial,** so a kill during the ad loses nothing.
- **The Daily has no revive,** because it's one fair attempt a day.
- **The tutorial** never reaches game over, and has no banner.

Left for 3.3 (with your AppLovin and AdMob accounts):
- An `IAdService` that wraps MAX, with retries on `RetrySchedule`.
- An `IConsentFlow` around MAX's terms and privacy flow with Google UMP. Its "Privacy Settings" button belongs in Settings, once it exists.
