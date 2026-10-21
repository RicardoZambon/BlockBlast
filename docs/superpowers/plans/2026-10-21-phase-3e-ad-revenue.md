# Phase 3e: Ad Revenue Tracking (3.5, App Side)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every paid ad impression, of every format, is logged as an `ad_impression` analytics event with its revenue. This happens behind `IAdService`, so the MAX wrapper (3.3) only has to pass on its `OnAdRevenuePaidEvent`.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 3.5 and the `ad_impression` row of 3.1's event table.

## The event

| Parameter | Value |
|---|---|
| `ad_platform` | "AppLovin" |
| `ad_source` | the network that showed the ad |
| `ad_format` | the format, as MAX names it ("INTER", "REWARDED", "BANNER") |
| `ad_unit_name` | the ad unit |
| `value` | the revenue |
| `currency` | "USD": MAX reports revenue in US dollars |

## Rules

- **Who reports:** `IAdService.RevenuePaid` reports each paid impression as an `AdRevenue` (network, format, ad unit, revenue). App logs it through the consent-gated analytics, like every other event.
- **Unknown revenue:** MAX reports -1 when it doesn't know the revenue. An impression with a negative, NaN or infinite revenue is not logged, since it would corrupt the totals. A revenue of 0 is logged.
- **Missing names:** a missing network or ad unit name is logged as "unknown".
- **Remove Ads:** owning it stops interstitials and the banner, not the rewarded revive, so revive impressions are still logged.

## Tasks

- [x] **The event's fields.**
  - `AdRevenueEvent.TryParameters(AdRevenue, out IReadOnlyDictionary<string, object>)` builds the table above.
  - It returns false for a revenue it shouldn't log.
  - Tested in EditMode by `AdRevenueTests`.
- [x] **The app logs it.** App subscribes to `RevenuePaid` once consent finishes, before the ad SDK starts, and `AnalyticsFlowTests` checks:
  - a paid impression is logged with its fields;
  - an unknown revenue is not logged.
- [x] **Docs:** this plan's Results, and the README's list of plans.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 149 EditMode and 75 PlayMode tests pass, and the 173 .NET tests are unchanged.
  - `AdRevenueTests` covers the fields, the unknown revenues (-1, NaN, infinity), a revenue of 0, and missing names.
  - `AnalyticsFlowTests` logs a paid rewarded impression and skips an unknown one.
  - Making App log an impression without the revenue check fails the second test, so it guards the rule.
- **Where it hooks in:**
  - `IAdService.RevenuePaid` is new. The stand-in never raises it, since no network pays it anything.
  - App subscribes in `StartAds`, after consent and before `Initialize`, and unsubscribes when destroyed.
  - The events go through the consent-gated analytics like the rest.
- **For the MAX wrapper (3.3):**
  - Raise `RevenuePaid` from `MaxSdkCallbacks.Interstitial/Rewarded/Banner.OnAdRevenuePaidEvent`.
  - Map `adInfo.NetworkName`, `adInfo.AdFormat`, `adInfo.AdUnitIdentifier` and `adInfo.Revenue` onto `AdRevenue`.
  - Firebase's `ad_impression` then shows in DebugView, which is the Phase 3 exit check.