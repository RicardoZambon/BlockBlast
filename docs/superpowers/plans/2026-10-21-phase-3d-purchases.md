# Phase 3d: Remove Ads and Restore Purchases (3.6, App Side)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The app side of master-plan task 3.6:
- one permanent Remove Ads purchase (`remove_ads_299`), whose ownership is stored on the phone and re-checked at every launch;
- a Restore Purchases button.

It sits behind `IStore`, with a stand-in until Unity IAP and the products exist in your App Store Connect and Play Console.

## Rules

- **Owning Remove Ads** turns off interstitials, by passing it to `AdPolicy`, and the banner. The rewarded revive stays, because the player chooses it.
- **Ownership at launch:** the store re-checks it every time, so a refund takes it away. If the store can't answer (offline, not ready), the saved value stays.
- **Settings** has the two buttons:
  - "Remove ads", showing the store's price once known, or "Ads removed", disabled, once owned.
  - "Restore purchases".
  - Both read in all four languages.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 143 EditMode and 73 PlayMode tests pass, and the 173 .NET tests are unchanged. `PurchaseFlowTests` checks:
  - buying stops interstitials and the banner, and changes the label;
  - a failed purchase changes nothing;
  - restore brings Remove Ads back;
  - a launch re-checks ownership, but keeps it when the store can't answer.
- **Still yours:** the `remove_ads_299` product in both store consoles, Apple's Paid Apps Agreement, and sandbox and license testers. With those, Claude wires in Unity IAP, with its receipt validation on the phone.
