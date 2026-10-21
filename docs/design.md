# Design: Block Puzzle (working title)

One page on what the game is, for anyone deciding on art, store text or tuning. The [master plan](superpowers/plans/2026-10-15-block-puzzle-game.md) has the why and the schedule; this page describes the game as it's built. Every number marked *(RC)* is a default that Remote Config can change once Firebase is in (3.1), so soft launch can tune it without a new build.

**In one line:** a calm 8×8 block puzzle with a Daily Puzzle everyone plays with the same pieces.

## Rules

- **Board and tray:** an 8×8 board and three pieces at a time. Pieces don't rotate.
- **Placing:** drag a piece onto the board. When all three are placed, three new ones are dealt.
- **Clearing:** full rows and columns clear together. A row and a column at once counts as 2 lines.
- **Game over:** when no piece in the tray fits anywhere.
- **Revive:** in Classic, once per game, the player can watch an ad to continue. They get a fresh tray, mostly small pieces, that all fit.

## Pieces

37 shapes in four size groups. Every orientation is its own shape.

| Group | Count | Shapes |
|---|---|---|
| Tiny | 1 | a single cell, dealt only in "mercy" deals and the revive |
| Small | 8 | lines of 2 and 3, small 3-cell corners |
| Medium | 19 | lines of 4, the 2×2 square, L, J, T, S and Z |
| Large | 9 | lines of 5, 2×3 and 3×2 rectangles, the 3×3 square, big 5-cell corners |

**Dealing**, from a seeded generator, so the same seed deals the same pieces on every phone:
- **Size by progress:** bigger pieces become more likely as a game goes on. Large pieces are 12% at the start, 18% from placement 30, and 25% from placement 90 *(RC)*.
- **Always playable:** at least one piece of every tray fits somewhere *(RC)*.
- **Helpful start:** for the first 15 placements, each slot has a 50% chance to prefer a piece that completes a line *(RC)*.
- **Mercy:** after 2 quick losses in a row (under 20 placements), the next game deals gentler pieces for 30 placements *(RC)*.
- **Profiles:** "gentle", "baseline" (the default) and "hard" *(RC)*. With the baseline, the simulator's greedy bot makes a median of 98 placements, and 0.1% of its games end before placement 20.

## Scoring *(RC)*

- **Cells:** 1 point per cell placed.
- **Lines:** n lines at once score 10 × n(n+1)/2: 10, 30, 60, 100…
- **Streak:** every placement that clears a line grows it, and 3 placements without a clear reset it. Line points are multiplied by 1 + 0.5 × (streak − 1).
- **Board clear:** +300.
- **Praise:** "Great!" for 2 lines, "Amazing!" for 3, "Incredible!" for 4 or more, "Board clear!", and "Streak n!" from a streak of 3.

## Modes at launch

- **Classic:** endless, chasing the best score, with the revive.
- **Daily Puzzle:** the game's own twist.
  - Everyone gets the same pieces each day (UTC), dealt from the date.
  - One scored attempt a day, and no revive.
  - A day streak.
  - A Share button that posts a one-line result (date, score, streak).
  - Setting the phone's clock back can't replay a day.
- **Tutorial:** a new player's first Play is three guided moves (a square, then a row and a column to clear), then straight into Classic.

Level-based modes wait until soft-launch data shows players want more.

## Ad rules *(RC)*

Copied from the master plan's Global Constraints:
- **Consent first:** no ad or analytics SDK collects anything before the consent flow finishes.
- **Interstitials:**
  - None during a new player's first 3 games.
  - At least 90 s between full-screen ads.
  - Only between games, never during one.
- **Rewarded:** one watch-an-ad-to-continue (the revive) per game, and only when an ad is loaded.
- **Banner:** at the top of the game screen, never next to the piece tray. Its slot is reserved from the start, so the board never moves.
- **Remove Ads** (US$2.99, bought once): removes banners and interstitials. The rewarded revive stays as an opt-in. Restore Purchases is in Settings.

## Art direction

- **Tone:** neutral and grown-up. No cartoon mascots or childish styling, which keeps the game outside Google Play's Families rules.
- **Board:** calm and dark, so bright solid blocks stand out. The prototype uses a deep navy background with blue blocks.
- **Readability:** board states are told apart by brightness, not only by colour. Text keeps 4.5:1 contrast and a minimum size.
- **Motion:**
  - A small squash when a piece lands, a sweep and particles when lines clear, and a light shake on big clears.
  - Haptics: light, medium and heavy.
  - Each one has an off switch, and reduced motion turns the movement off.
- **Sound:** short and soft. Notes rise in pitch as a streak grows, and music is optional.
- **Assets:** a good asset pack plus a custom app icon is enough (master plan 2.1). Keep every licence in `legal/licenses/`.

**Reference images** (yours to pick, with the art in 2.1). Add three here, one for each:
1. **Blocks:** the block skin, solid colours with a soft bevel or gloss that reads at a glance.
2. **Board and background:** a quiet backdrop that never competes with the pieces.
3. **UI kit:** buttons and panels in the same family, readable in all four languages.

Pick original work you have the rights to, and nothing from other block games: the store rules forbid copying their look or names.

## KPIs

What soft launch (Phase 5) is judged on. These are rules of thumb for the genre; check them against GameAnalytics' benchmarks.

| Metric | Rework or stop | Good enough to grow | Strong |
|---|---|---|---|
| Day-1 retention | < 30% | 35–40% | ≥ 45% |
| Day-7 retention | < 10% | 12–15% | ≥ 20% |
| Day-30 retention | < 4% | 5–8% | ≥ 10% |
| Playtime per daily player | < 12 min | 20+ min | 30+ min |
| Crash-free users | < 99% | ≥ 99.5% | ≥ 99.8% |

Watched weekly alongside these:
- games per daily player;
- ad impressions per daily player, by format;
- eCPM, by network and country;
- ARPDAU, by country;
- the rewarded opt-in rate;
- Remove Ads conversion.

Performance targets, from day one:
- 60 fps on a low-end Android phone.
- Playable within 3 s of a cold start.
- An Android download under 80 MB.
