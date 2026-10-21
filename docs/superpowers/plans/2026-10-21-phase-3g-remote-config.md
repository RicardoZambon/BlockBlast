# Phase 3g: Remote Config, App Side (3.1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The master plan's Remote Config keys, with the same defaults bundled in the app so it plays the same offline. Fetched values tune new Classic games, the ads, the revive and the rating prompt, without a new build. It sits behind `IRemoteConfig`, with a stand-in until Firebase is wired in with your project (3.1).

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 3.1's Remote Config table, 5.4 (weekly tuning), and the Global Constraints' privacy rule.

## The keys

| Key | Default | Feeds |
|---|---|---|
| `gen_profile` | `baseline` | the generator preset: `gentle`, `baseline` or `hard` |
| `gen_fit_guarantee` | the profile's | `none`, `at_least_one` or `whole_tray` |
| `gen_helpful_until_placement` | the profile's (15 for baseline) | the helpful start |
| `gen_mercy_after_quick_losses` | 2 | mercy |
| `score_line_base` | 10 | points for a single line |
| `score_board_clear_bonus` | 300 | the board-clear bonus |
| `ads_inter_enabled` | `true` | interstitials on or off |
| `ads_inter_grace_games` | 3 | games before the first interstitial |
| `ads_inter_cooldown_s` | 90 | seconds between full-screen ads |
| `ads_banner_enabled` | `true` | the banner on or off |
| `ads_revive_enabled` | `true` | the rewarded revive on or off |
| `iap_remove_ads_sku` | `remove_ads_299` | the product the IAP wrapper sells (3.6) |
| `review_prompt_min_games` | 5 | the earliest game for the rating prompt |

## Rules

- **Defaults:** without a fetched value the app plays exactly as before. The defaults are the table's.
- **Bad values:** a value that doesn't parse, or that the engine's checks reject, falls back to its default. That's decided key by key, so one typo doesn't undo the rest.
- **Consent first:** the fetch happens once the consent flow finishes, like the ads and analytics.
- **A game keeps its tuning:**
  - New values apply to the next new Classic game.
  - A game in progress keeps the generator and scoring it started with, even after a relaunch. The save holds them.
  - A save from before this change resumes with the baseline, as it did.
- **Fair for everyone:** the Daily and the tutorial ignore Remote Config, since every phone must deal the same Daily.
- **Analytics:** `game_start` logs the game's real `gen_profile`.

## Tasks

- [x] **RemoteTuning.** `RemoteTuning.From(Func<string, string?> value)` reads the keys, and `RemoteTuning.Defaults` holds the table's defaults. `RemoteTuningTests` checks:
  - the defaults;
  - each key's parsing;
  - per-key fallback for unparsable, out-of-range and engine-rejected values;
  - unknown profiles.
- [x] **The app side.** `IRemoteConfig` (`Fetch(Action done)` and `Value(key)`) has a stand-in with no values. App fetches after consent and applies the result. `RemoteConfigFlowTests` checks:
  - no fetch before consent;
  - a fetched profile tunes the next new game and its `game_start`;
  - a game in progress keeps its tuning across a relaunch;
  - the switches for interstitials, the banner and the revive;
  - the Daily stays on the baseline.
- [x] **Docs:** this plan's Results, and the README's list of plans.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 189 EditMode, 87 PlayMode and 174 .NET tests pass.
  - `RemoteTuningTests` (20) covers the defaults, every key, and 16 bad values that each keep their default.
  - `ClassicModeTests` (3) covers a damaged saved tuning, a save from before this change, and a new game saving its tuning.
  - `RemoteConfigFlowTests` (7) has the cases above.
  - `RetentionRulesTests` adds the rating minimum from Remote Config, and `GameSessionTests` adds `GeneratorProfile`.
  - Making `Resume` use the current tuning instead of the saved one fails the keep-its-tuning test.
- **Engine:** `GameSession.GeneratorProfile` names the settings a game deals with. `game_start` uses it, instead of always saying "baseline".
- **Save:** `SaveData` holds `GameGenerator` and `GameScoring` for the game in progress. An old save loads with the baseline, and damaged settings fall back to it.
- **App:**
  - It fetches once consent finishes, then applies the result at once.
  - The banner is re-checked straight away; new Classic games, the interstitial policy, the revive offer and the rating prompt read the tuning when they need it.
- **Value formats:** booleans are "true" or "false" in any case, and numbers are whole numbers within sane limits (for example, at most 86,400 s of cooldown). A product ID uses lowercase letters, digits, `_` and `.`, as both stores allow.
- **For the Firebase wrapper (3.1):**
  - `Fetch` calls `FetchAsync` and `ActivateAsync`.
  - `Value` returns `ConfigValue.StringValue` for a key whose source is remote, and null otherwise, so the bundled defaults stay in one place.
- **Left for later:**
  - `iap_remove_ads_sku` is parsed but has no user until Unity IAP (3.6). Ownership must then cover every SKU the game ever sold.
  - Firebase A/B Testing works through these same keys once Firebase is in.