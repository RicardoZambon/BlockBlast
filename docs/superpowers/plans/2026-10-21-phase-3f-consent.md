# Phase 3f: Consent Results and Privacy Settings (3.2, App Side)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The app side of master-plan task 3.2:
- What the consent flow finds out reaches analytics before its first event, and goes into `consent_result`.
- Settings offers Privacy Settings where the player's region requires it.

It all sits behind `IConsentFlow`, with a stand-in until Google's form (UMP) comes with MAX (3.3).

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 3.2, the `consent_result` row of 3.1's table, and 2.3's Settings list.

## Rules

- **What the flow reports:** a `ConsentResult`:
  - whether GDPR applies (unknown until Google's form says);
  - the player's answer to Apple's tracking prompt;
  - the four switches of Firebase's consent mode: `analytics_storage`, `ad_storage`, `ad_user_data`, `ad_personalization`.
- **Consent mode first:** analytics gets the result before its first event. It gets it again whenever the player changes their choices.
- **The event:** `consent_result` logs `gdpr_applies` ("true", "false" or "unknown") and `att_status`. It's logged at every launch and after every change in Privacy Settings.
- **Privacy Settings:**
  - Shown only where the region requires a way back into the form (GDPR).
  - It reopens the form, and the new choices go to analytics, with a new `consent_result`.
- **The stand-in knows nothing:**
  - All four switches are denied, so a Firebase build without Google's form stores nothing.
  - Privacy Settings stays hidden.
- **Settings has to fit:** found while placing the new button. On a 3:4 iPad the canvas is only about 1660 units tall, and "Reduced motion" overlapped "Remove ads".

## Tasks

- [x] **Settings fits every screen.** One column from the top, with Back and the version at the bottom. `ViewTests` checks that no row overlaps another or leaves the screen at 9:16, 9:21 and 3:4. It failed at 3:4 before the fix.
- [x] **ConsentResult.** `ConsentResultTests` checks:
  - the stand-in's answer (everything denied, no Privacy Settings);
  - the event's parameters.
- [x] **The app side.** `ConsentFlowTests` checks:
  - the result reaches analytics before `consent_result`;
  - the event's values;
  - Privacy Settings shown where required, and hidden (and inert) elsewhere;
  - reopening the form passes the new choices on.
- [x] **Docs:** this plan, and the README's list of plans.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 165 EditMode and 80 PlayMode tests pass, and the 173 .NET tests are unchanged.
  - The layout test failed at 3:4 before the fix ("reduced_motion overlaps Remove ads").
  - Taking the consent hand-off out of `ConsentGatedAnalytics.Open` fails the ordering test.
- **Interfaces:**
  - `IConsentFlow.Request` now passes a `ConsentResult`.
  - It also gained `PrivacyOptionsRequired` and `ShowPrivacyOptions`.
  - `IAnalytics` gained `SetConsent`.
  - `ConsentGatedAnalytics.Open(consent)` passes the consent on before letting events through.
- **Settings:** Privacy Settings sits under Restore purchases, in all four languages: "Privacy settings", "Configurações de privacidade", "Configuración de privacidad" and "Pengaturan privasi".
- **For the wrappers:**
  - **MAX and UMP (3.3):**
    - Fill `ConsentResult` from the flow: where the player is, Apple's answer and the consent choices.
    - Report UMP's privacy options requirement.
    - Show UMP's privacy options form from `ShowPrivacyOptions`.
  - **Firebase (3.1):** pass the four switches to `FirebaseAnalytics.SetConsent` in `SetConsent`.
- **Left for later:**
  - The Support and Privacy Policy links from 2.3's Settings list wait for the website (0.5).
  - The iOS tracking text (`NSUserTrackingUsageDescription`) comes with MAX's settings.
  - The age-signal APIs come before the US launch.