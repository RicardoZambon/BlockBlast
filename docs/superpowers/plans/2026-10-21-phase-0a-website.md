# Phase 0a: The Website (0.5)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Master-plan task 0.5's one-page website, written now with placeholders, so that publishing only needs your name, domain, email and ad lines:
- a home page with support;
- a privacy policy;
- terms of use;
- `app-ads.txt`.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md):
- 0.5;
- the Global Constraints (privacy, the 13+ audience, the domain);
- 2.3's Settings list;
- 3.2 (consent and US state privacy);
- 4.5 (Data safety).

## Rules

- **Collects nothing itself:** no scripts, frames, fonts, images or styles from elsewhere, and no cookies. The `site` workflow checks it.
- **Says what the game does:**
  - Progress stays on the phone.
  - Ads go through AppLovin MAX and the networks the plan names.
  - Firebase handles analytics, crashes and settings.
  - Nothing is collected before the consent request is answered.
  - The choices match the game: Privacy Settings in GDPR regions, the US state opt-out, Apple's tracking prompt, and Android's advertising ID.
- **Placeholders, not guesses:**
  - Every value only you can give is a `[bracketed]` placeholder: name, seller, email, address, links, dates, governing law, and Firebase's retention period.
  - The publishing job refuses to run while any is left.
- **Publishing is yours:**
  - The deploy job waits for GitHub Pages to be turned on and a `PAGES_ENABLED` repository variable.
  - Publishing puts the pages on the public web, so it's your call.
- **Drafts:** the privacy policy and terms say so in a comment, for a lawyer's read before launch.

## Tasks

- [x] `site/`: `index.html`, `privacy.html`, `terms.html`, `style.css` (light and dark), `app-ads.txt` (instructions only).
- [x] `.github/workflows/site.yml`: the checks on every change, and the gated deploy with the placeholder guard.
- [x] The README's "Website" section: the three steps to publish.

## Results

Written on 2026-10-21.
- **Checks:** the workflow's two checks pass locally (no outside resources, every local link exists). They'll run on the pull request too.
- **Placeholders left:** `[Game name]` (12), `[Support email]` (8), `[Seller name]` (5), `[Date]` (2), and one each of `[App Store link]`, `[Google Play link]`, `[Year]`, `[Postal address]`, `[Country or state]` and `[Retention period]`.
- **Not done:**
  - Store badges, which need the store pages.
  - Translations of the policy, which can follow the store listings (4.3).
  - The Support and Privacy Policy links inside the game's Settings (2.3), which need the domain.
