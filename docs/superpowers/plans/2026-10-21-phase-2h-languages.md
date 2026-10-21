# Phase 2h: Three More Languages (Translations, Step 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The second step of master-plan task 2.8. Brazilian Portuguese, Latin-American Spanish and Indonesian sit behind the keys from [Phase 2g](2026-10-21-phase-2g-strings.md), picked from the phone's language.

## Global Constraints

- **The phone's language picks the table:**
  - Unity reports any Portuguese as Portuguese, so Brazil's table is used for it.
  - Spanish gets the Latin-American table, and Indonesian its own.
  - Every other language gets English.
- **Fallbacks:** a key missing from a translation shows English, and one missing from English shows itself.
- **Every translation keeps its key's placeholders,** so a count is never dropped or doubled.
- **Tests always run in English** (`App.Boot`'s language argument), so they pass on a phone or PC in any language.
- **The translations are drafts.** A native speaker reviews each before launch. That's your part of 2.8.

## Tasks

- [ ] **Tests first** (`StringsTests`):
  - every language has every key with the same placeholders;
  - the phone's language picks the table;
  - each language reads its own table.
- [ ] **Implement:** `Language`, the three dictionaries, `Strings.Current` and `FromSystem`, and `App.Boot`'s language.
- [ ] **Run the suites;** commit.

## Results

Built on 2026-10-21, test-first.
- **Tests:** 128 EditMode and 52 PlayMode tests pass.
- **Coverage:** every language has all 37 keys, with the same placeholders as English.
- **Font:** the built-in font has the accented Latin letters these languages need (á, ã, ç, é, í, ñ, ó, õ, ú, ¡).
- **Still yours:** a native speaker's review of each draft. Also the choice between keeping this code table and moving it into Unity's Localization package. That package needs Addressables in the build; for about 40 strings the table is simpler, and the keys carry over either way.
