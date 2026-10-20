# Block Puzzle Game Implementation Plan (iOS + Android, ad-funded)

> **For agentic workers:** This is the master plan. Before starting an engineering phase (1–4), expand it into a step-by-step TDD plan with superpowers:writing-plans. Then use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement it task by task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an original 8×8 block puzzle game on iOS and Android that earns money from ads plus a remove-ads purchase. Grow it only as fast as players earn back what they cost to acquire.

**Architecture:** The game rules live in a small, pure C# engine (the whole board is one 64-bit integer), shared by the Unity app and a .NET simulator. The simulator plays millions of bot games to tune the piece generator before any person sees it. The Unity client adds the visuals, touch input and a thin layer of services: consent → ads → analytics and remote config → purchases. Every tuning value (difficulty, how often ads appear, prices) lives in Firebase Remote Config, so it can be A/B tested without shipping a new build.

**Tech Stack:**
- **Game:** Unity 6.3 LTS (C# 9)
- **Engine tests and simulator:** .NET 10
- **Ads:** AppLovin MAX choosing between ad networks (AppLovin, Google bidding, Unity Ads, Mintegral, Liftoff); Google's consent form (UMP) shown through MAX's built-in flow
- **Purchases:** Unity IAP
- **Analytics and tuning:** Firebase Analytics, Crashlytics, Remote Config, A/B Testing
- **Build automation:** GitHub Actions + GameCI + fastlane

## Global Constraints

- **Identity:**
  - Original name, art, UI and sounds.
  - Never use "Block Blast" or any other game's name or trademark in the app or its store metadata (App Store Review Guidelines 2.3.7 and 4.1).
- **Platforms:** iPhone, iPad, Android phones and tablets. Portrait only.
- **Minimum OS:** iOS 15 and Android 7.1 (API 25). Unity 6.3 supports nothing lower, so API 24 (Android 7.0) was raised. Raise them only if an SDK requires it.
- **Store build rules:**
  - Android `targetSdkVersion` 36, required for new apps and updates since 2026-08-31.
  - iOS builds made with Xcode 26 or later against the iOS 26 SDK, required since 2026-04-28.
- **Engine code:**
  - Plain C# 9 (the level Unity's compiler supports), no `UnityEngine` references.
  - Fully deterministic for a given seed.
  - Never use `System.Random` in game logic. Use the engine's own seeded generator so every platform deals the same pieces.
- **Performance:**
  - 60 fps on a low-end Android phone (2–3 GB RAM).
  - Playable within 3 s of a cold start.
  - Android download under 80 MB.
- **Offline:** the whole game works offline; ads and analytics simply don't load.
- **Privacy:**
  - No ad or analytics SDK collects data before the consent flow finishes.
  - Target audience 13+ (not "Designed for Families").
  - Privacy policy, terms and `app-ads.txt` hosted on your own domain.
- **Ad rules** (defaults, all in Remote Config):
  - No full-screen ad between games (interstitial) during a new player's first 3 games.
  - At least 90 s between full-screen ads.
  - Interstitials only between games, never during one.
  - One watch-an-ad-to-continue (rewarded revive) per game.
  - Banner at the top of the game screen, never next to the piece tray.
- **Purchases at launch:**
  - One permanent "Remove Ads" purchase (a non-consumable). It removes banners and interstitials; rewarded ads stay available as an opt-in.
  - A Restore Purchases button.
- **Secrets:**
  - Signing keys, keystores and API keys are never committed.
  - Back up the Android upload keystore and its passwords in two separate places.

## Assumptions (change these and the plan changes)

| Topic | Assumed | Why / alternative |
|---|---|---|
| Team | You, solo, with C# experience. Claude Code can help with code. Art and audio are bought or commissioned | A second person on art would roughly halve Phase 2 |
| Time | Two tracks: full-time (~40 h/week) or part-time (~15 h/week) | The durations below show both |
| Engine | **Unity 6.3 LTS** (supported to Dec 2027), or the newest LTS in Unity Hub when you start | See "Why Unity" below |
| Ad mediation | **AppLovin MAX** | Strong demand from game advertisers, a maintained consent flow (Google UMP + Apple's tracking prompt), revenue data for every ad shown, and built-in A/B tests for ad settings. Fallback: Unity LevelPlay |
| Dev machine | Windows | iOS builds need a Mac running Xcode 26: your own, a Mac mini, a rented cloud Mac or a macOS CI runner |
| Working title | "Block Puzzle", bundle ID `com.<yourstudio>.blockpuzzle` | Replaced by the real name in task 0.2 |

**Why Unity:**
- It uses C#, the same language as your .NET work.
- Every ad network, Firebase and both stores' purchase systems have official Unity plugins.
- Unity Personal is free while revenue plus funding stays under US$200K over the last 12 months, and in Unity 6 the "Made with Unity" splash screen is optional.
- **Alternatives:**
  - Godot 4 is lighter, but its ad plugins are maintained by the community. That's risky for the part that makes the money.
  - Flutter + Flame also works, but means writing Dart.

## Timeline at a glance

| Phase | You end up with | Full-time | Part-time |
|---|---|---|---|
| 0. Decide & set up | Accounts, name, design doc, a blank app running on real phones | 1 week | 2–3 weeks |
| 1. Engine & prototype | Rules engine, simulator, a rough playable version | 3 weeks | 7–8 weeks |
| 2. Game feel & content | Art, visual and sound feedback, tutorial, Daily Puzzle, save/resume, 4 languages | 3–4 weeks | 8–10 weeks |
| 3. Money & services | Consent, ads, purchases, analytics, remote config | 2–3 weeks | 5–7 weeks |
| 4. QA & store readiness | Tested builds, store listings, compliance forms, approvals | 1–2 weeks | 3–4 weeks |
| 5. Soft launch | Real retention and revenue data from 1–2 countries; a tuned game | 4–8 weeks | 4–8 weeks |
| 6. Global launch & growth | Worldwide release; paid installs only if the numbers work | ongoing | ongoing |
| 7. Live ops | Updates, events, experiments, yearly compliance | ongoing | ongoing |

Soft launch lands around week 10–13 full-time, or month 6–8 part-time. If you start in November 2026, that's about February 2027 (full-time) or May–June 2027 (part-time).

The first ad revenue arrives during soft launch, once AdMob approves the live app. The first payouts come 1–2 months after that.

Google Play's closed test (task 2.10) must run for at least 14 days, so start it at the end of Phase 2 and let it run alongside Phase 3.

---

## Phase 0: Decide & set up

**Goal:** every account, decision and tool that later phases depend on, plus proof that you can put a build on a real iPhone and a real Android phone.

- [ ] **0.1 Choose the twist.**
  - The core loop stays standard: 8×8 board, three pieces at a time, no rotation, full rows and columns clear.
  - The game needs one feature of its own so it isn't a copy (and isn't rejected as one).
  - Recommended: a **Daily Puzzle**. Each day's pieces are dealt from the date, so everyone gets the same game, and players can share their result.
    - It reuses the seeded piece generator.
    - It gives players a reason to come back every day.
    - It gives them something to post.
  - Pair it with one distinct visual theme.
  - Leave level-based modes until soft-launch data shows players want more.
- [ ] **0.2 Name and identity.**
  - Make a shortlist of 10 names and search both stores for clashes.
  - Run trademark searches: USPTO (US), EUIPO (EU) and your own country's office.
  - Buy the domain and reserve social media handles for the winner.
  - Avoid names close to big titles (for example, anything with "blast").
- [ ] **0.3 Decide who the seller is: you personally, or a company.**
  - The stores show the seller's name publicly.
  - Once the app earns money from ads or purchases, the EU treats you as a "trader". Apple then publishes your address, phone number and email on your EU product pages (EU Digital Services Act).
  - Company accounts need a D-U-N-S number. It's free, but allow 1–2 weeks.
  - Google Play **personal** accounts created after 13 Nov 2023 must run a closed test with at least 12 testers for 14 straight days before going live. Company accounts are exempt.
- [ ] **0.4 Create the accounts:**
  - Apple Developer Program: US$99 a year.
  - Google Play Console: US$25 once, plus identity verification.
  - AdMob, AppLovin (for MAX) and a Unity ID.
  - A Firebase project with one iOS app and one Android app.
  - Set up payouts and tax:
    - Apple: accept the Paid Apps Agreement and add bank details and tax forms.
    - Google Play: create a payments profile.
    - AdMob and AppLovin: add payment and tax details.
    - Outside the US, fill in a W-8BEN (individual) or W-8BEN-E (company) wherever asked.
- [ ] **0.5 Website.** A static one-page site on your domain (GitHub Pages hosting is free) with:
  - a privacy policy
  - terms of use
  - a support email
  - `app-ads.txt` at the root of the domain. AdMob checks this file before it lets a new app show ads at full volume.
- [ ] **0.6 Tools and devices:**
  - Unity Hub with Unity 6.3 LTS, including Android Build Support and iOS Build Support
  - the .NET 10 SDK, plus Rider or Visual Studio
  - Git with Git LFS (for art and audio files); host the repository on GitHub, which GameCI needs later
  - a Mac with Xcode 26 and CocoaPods
  - one low-end Android phone (2–3 GB RAM) and one iPhone
- [ ] **0.7 One-page design doc** in `docs/design.md`:
  - rules, piece list and scoring
  - modes at launch: Classic + Daily
  - ad rules, copied from Global Constraints
  - art direction, with 3 reference images
  - the KPIs from Phase 5
- [ ] **0.8 Prove the pipeline.**
  - Create the Unity project from the "Universal 2D" template and set the bundle IDs.
  - Build and run a blank scene on both phones.
  - Signing problems should show up now, not in week 10.

**Exit:** all accounts active, name cleared, design doc written, blank app running on both phones.

---

## Phase 1: Engine, simulator & rough prototype

**Goal:** a correct, tested rules engine; a tuned piece generator; and a playable but plain version on both phones.

### Repository layout

```
block-puzzle/
├─ core/
│  ├─ src/                         # engine source; doubles as a local Unity package
│  │  ├─ package.json              # com.<yourstudio>.blockpuzzle.core
│  │  ├─ BlockPuzzle.Core.asmdef   # "noEngineReferences": true
│  │  ├─ Board.cs                  # 64-bit board: fits, place, clear
│  │  ├─ PieceShape.cs             # one shape + its precomputed placement masks
│  │  ├─ PieceCatalog.cs           # the full piece set, size categories, base weights
│  │  ├─ Pcg32.cs                  # seeded random generator, same output on every platform
│  │  ├─ GeneratorSettings.cs      # tuning values (filled from Remote Config)
│  │  ├─ PieceGenerator.cs         # deals trays of 3 pieces
│  │  ├─ Scoring.cs                # points, multi-line bonus, streaks
│  │  ├─ GameSession.cs            # one game: tray, place, score, game over
│  │  ├─ GameSnapshot.cs           # state for save/resume
│  │  └─ AdPolicy.cs               # decides when an ad may show (pure logic, testable)
│  ├─ dotnet/
│  │  ├─ BlockPuzzle.Core/         # BlockPuzzle.Core.csproj: netstandard2.1, LangVersion 9.0, compiles ../../src/**/*.cs
│  │  ├─ BlockPuzzle.Core.Tests/   # xUnit, net10.0
│  │  └─ BlockPuzzle.Sim/          # console app: bots play N games and print stats
│  └─ BlockPuzzle.slnx
├─ unity/                          # Unity 6.3 LTS project
│  ├─ Packages/manifest.json       # "com.<yourstudio>.blockpuzzle.core": "file:../../core/src"
│  └─ Assets/Game/
│     ├─ Presentation/             # board/tray views, drag & drop, effects, audio, haptics
│     ├─ UI/                       # Home, Game, GameOver, Settings screens
│     ├─ Services/                 # Ads, Consent, Purchases, Analytics, RemoteConfig, Save
│     └─ Tests/                    # Unity EditMode/PlayMode tests
└─ docs/design.md
```

The engine's `.csproj` lives outside `core/src`, so its `bin/` and `obj/` folders never end up inside the Unity package (otherwise Unity would try to import those DLLs). Each project has its own folder, so the engine project never picks up the tests' or the simulator's source files.

### The engine core (sketch)

```csharp
namespace BlockPuzzle.Core
{
    // 8x8 board in one 64-bit integer. Bit index = row * 8 + col.
    public readonly struct Board
    {
        public readonly ulong Cells;
        public Board(ulong cells) { Cells = cells; }

        // 8 row masks followed by 8 column masks.
        static readonly ulong[] LineMasks = BuildLineMasks();

        public bool Fits(ulong pieceMask) => (Cells & pieceMask) == 0;

        public PlaceResult Place(ulong pieceMask)
        {
            ulong filled = Cells | pieceMask;
            ulong cleared = 0;
            int lines = 0;
            foreach (ulong line in LineMasks)
            {
                if ((filled & line) == line) { cleared |= line; lines++; }
            }
            return new PlaceResult(new Board(filled & ~cleared), lines, cleared);
        }

        static ulong[] BuildLineMasks()
        {
            var masks = new ulong[16];
            for (int i = 0; i < 8; i++)
            {
                masks[i] = 0xFFUL << (i * 8);               // row i
                masks[8 + i] = 0x0101010101010101UL << i;   // column i
            }
            return masks;
        }
    }

    public readonly struct PlaceResult
    {
        public readonly Board Board;
        public readonly int Lines;
        public readonly ulong ClearedMask;   // lets the view animate exactly the cleared cells
        public PlaceResult(Board board, int lines, ulong clearedMask)
        {
            Board = board; Lines = lines; ClearedMask = clearedMask;
        }
    }
}
```

- Each piece precomputes one bit pattern (mask) for every top-left position where it fits inside the board; there are at most 64.
- Checking whether a piece fits anywhere takes at most 64 bitwise ANDs.
- The game is over when no piece in the tray fits anywhere.
- Full rows and columns are cleared together, so a cross-shaped clear counts as two lines.

### Tasks

- [ ] **1.1 Project setup:**
  - Create the `core/` layout above, with an xUnit test project and one passing test.
  - Wire up the Unity package: `package.json`, the `.asmdef` with `noEngineReferences`, and the `manifest.json` entry.
  - Add a GitHub Actions workflow that runs `dotnet test` on every push.
- [ ] **1.2 Board.** Tests cover:
  - placing a piece inside the board
  - rejecting a piece that overlaps filled cells
  - clearing a single row, and a single column
  - clearing a row and a column at once: counts as 2 lines, and the shared cell is cleared once
  - clearing the whole board
- [ ] **1.3 Piece catalog: about 35–40 shapes.** Pieces don't rotate, so every orientation is its own shape:
  - straight lines 2–5 cells long, horizontal and vertical
  - 2×2 and 3×3 squares; 2×3 and 3×2 rectangles
  - small 3-cell corners
  - 4-cell L/J, T and S/Z shapes
  - 5-cell big corners
  - optionally a single cell, used only for easier "mercy" deals

  Define shapes from text art (`"##."`, `".##"`) so they're easy to read and review.

  Tests check that:
  - every placement stays inside the board
  - each shape has the expected number of positions (a 3×3 square has 36)
- [ ] **1.4 Scoring.** Defaults, all in Remote Config:
  - 1 point per cell placed.
  - Clearing *n* lines at once scores 10 × n(n+1)/2: 10, 30, 60, 100…
  - A streak multiplier of ×(1 + 0.5 × (streak − 1)). The streak grows with every placement that clears a line and resets after 3 placements without a clear.
  - +300 for clearing the whole board.
- [ ] **1.5 Seeded random generator + piece generator v1.**
  - **Random generator:** use PCG32. It's tiny and fast, and it gives identical results on every platform, which the Daily Puzzle needs.
  - **Piece generator:** picks pieces by size category, weighted by how high the score is (bigger pieces as the score grows).
  - **Generator settings:**
    - **Fit guarantee.** Default: at least one of the three pieces fits. Option: all three fit in some order.
    - **Helpful bias.** Early in a game, prefer pieces that would complete a line.
    - **Mercy.** After 2 quick losses in a row, deal the next game more gently.
  - **Tests** use fixed seeds and check that deals are reproducible.
- [ ] **1.6 GameSession + GameSnapshot.**
  - `GameSession` runs one game: `TryPlace(traySlot, row, col)`, scoring, streaks and game-over detection.
  - `GameSnapshot` holds the board, tray, score, streak, random-generator state and game index, saved as JSON.
  - Test: save, load, and the game deals exactly the same pieces afterwards.
- [ ] **1.7 Simulator.**
  - **Two bots:** one plays random legal moves; one is greedy (clears the most lines, then leaves the fewest isolated empty cells).
  - **Volume:** plays 100,000 games per generator profile.
  - **Prints:**
    - moves per game (median, P10, P90)
    - the share of games that end before move 20
    - lines cleared per game
    - longest streaks
    - how often mercy kicks in
  - **Starting targets** for the "baseline" profile with the greedy bot: a median of 80–150 placements, and under 5% of games ending before move 20.
  - Recalibrate after playtests with people: aim for Classic games of 3–8 minutes.
- [ ] **1.8 Rough Unity prototype:**
  - Draw the board and tray with plain squares.
  - Drag and drop: the piece floats about 1.5 cells above the finger and grows to board size while dragging.
  - Snap to the nearest legal spot, show a ghost preview, and highlight the lines that will clear.
  - A drop on an invalid spot animates the piece back to the tray.
  - Game over leads to a restart.
  - Save after every move; resume after the app is killed.
  - Set `Application.targetFrameRate = 60` and don't allocate memory inside the drag loop.
- [ ] **1.9 Playtest with 5–10 people.**
  - Hand them the phone, say nothing, and watch.
  - Note where they hesitate, how long games last, and whether they start a second game without being asked.

**Exit (Gate A):**
- Most testers start another game without being asked.
- The simulator numbers are on target.
- It runs at 60 fps on the low-end Android phone. Measuring it doesn't hold up Phase 2: without a phone at hand, it's checked in 4.2 at the latest.

If people don't replay, fix the feel and the piece generator before adding art.

---

## Phase 2: Game feel & content

**Goal:** a version you'd be happy for strangers to play: a polished Classic mode, the Daily Puzzle, a tutorial, and four languages.

- [ ] **2.1 Art.**
  - **What's needed:** block skin, board, background, UI kit and app icon. A good asset pack plus a custom icon is enough; commission custom art only if the budget allows.
  - **Style:** keep it neutral and grown-up, with no cartoon mascots aimed at kids, so the game stays outside Google Play's Families rules.
  - **Records:** store every asset licence and receipt in `legal/licenses/`, in case anyone ever files a copyright claim.
- [ ] **2.2 Visual and sound feedback:**
  - a small squash when a piece lands, and a sweep animation when lines clear
  - particles, a streak counter and short praise words
  - a very light screen shake on big clears
  - vibration (haptics): light when placing, medium when clearing, heavy when the whole board clears
  - sounds that rise in pitch as a streak grows
  - toggles in Settings for sound, music, haptics and reduced motion
- [ ] **2.3 Screens:**
  - **Home:** Play, Daily, Settings.
  - **Game:** score, best score, pause.
  - **Game over:** score, best score, a spot for the revive offer, Play Again.
  - **Settings:** the toggles, Privacy Settings, Restore Purchases, Support, Privacy Policy, version number.
- [ ] **2.4 First-time tutorial.** Two or three guided moves on a preset board, with a hand pointer and almost no text. Returning players skip it.
- [ ] **2.5 Daily Puzzle:**
  - the seed comes from the UTC date
  - one scored attempt per day
  - a day-streak counter stored on the phone
  - a share button that posts a short text result: score, streak and a link
- [ ] **2.6 Reliable save and resume.**
  - Save after every move.
  - Handle the app going to the background, incoming calls, the OS killing the app, and the phone's clock changing (which matters for the Daily).
- [ ] **2.7 Layout.**
  - Respect safe areas (notches).
  - Support screen shapes from 4:3 (iPad) to 21:9 (tall Android phones).
  - Reserve the banner slot at the top from day one, so turning ads on or off never moves the board under a player's finger.
- [ ] **2.8 Translations:** English, Brazilian Portuguese, Latin-American Spanish and Indonesian, using Unity's Localization package (`com.unity.localization`). These match the genre's biggest markets (Indonesia, the US, Brazil), and the game has little text, so translation is cheap.
- [ ] **2.9 Accessibility:**
  - pieces readable by brightness, not only by color
  - a minimum text size
  - checked contrast
  - an "off" switch for every effect
- [ ] **2.10 Start the Google Play closed test now** (personal accounts only).
  - Upload a build to a closed testing track.
  - Invite at least 12 testers who will actually play: friends, colleagues, online communities.
  - Keep at least 12 of them opted in for 14 days in a row. If the count drops below 12, the 14 days start over.
  - Send them updated builds as Phase 3 progresses.

**Exit:**
- Classic and Daily are feature-complete.
- Someone who doesn't play games can play without help.
- The closed test is running.

---

## Phase 3: Money & services

**Goal:** consent, ads, purchases, analytics and remote tuning all working on both platforms, in test mode.

### 3.1 Firebase

- [ ] Add Firebase Analytics, Crashlytics, Remote Config and A/B Testing using the Firebase Unity SDK.
  - Firebase and MAX both use Google's External Dependency Manager for Unity (EDM4U), which fetches their native Android and iOS libraries. Keep a single copy of it.
  - Turn on Unity's custom Gradle templates.
  - Run CocoaPods on the Mac for the iOS dependencies.
- [ ] Analytics events to log:

| Event | When | Parameters |
|---|---|---|
| `tutorial_begin` / `tutorial_complete` | first-time tutorial | none |
| `game_start` | new game | `mode` (classic/daily), `game_index`, `gen_profile` |
| `game_end` | game over or quit | `mode`, `score`, `placements`, `lines`, `max_streak`, `duration_s`, `revived` |
| `revive_offer` / `revive_accept` | game-over screen | `game_index` |
| `daily_complete` | Daily finished | `score`, `streak_days` |
| `share` | share tapped | `content_type` |
| `consent_result` | consent flow finished | `gdpr_applies`, `att_status` |
| `ad_impression` | every paid ad impression (see 3.5) | `ad_platform`, `ad_source`, `ad_format`, `ad_unit_name`, `value`, `currency` |
| purchases | automatic | Firebase logs `in_app_purchase` by itself |

- [ ] Remote Config keys and their defaults. Bundle the same defaults inside the app so it works offline:

| Key | Default | Controls |
|---|---|---|
| `gen_profile` | `baseline` | generator preset: `gentle`, `baseline` or `hard` |
| `gen_fit_guarantee` | `at_least_one` | `none` / `at_least_one` / `whole_tray` |
| `gen_helpful_until_placement` | `15` | early-game preference for pieces that complete a line |
| `gen_mercy_after_quick_losses` | `2` | an easier deal after N games shorter than 20 placements |
| `score_line_base` | `10` | points for a single line |
| `score_board_clear_bonus` | `300` | bonus for clearing the whole board |
| `ads_inter_enabled` | `true` | interstitials on/off |
| `ads_inter_grace_games` | `3` | no interstitials during a new player's first N games |
| `ads_inter_cooldown_s` | `90` | minimum seconds between full-screen ads |
| `ads_banner_enabled` | `true` | banner on/off (A/B test this against retention) |
| `ads_revive_enabled` | `true` | rewarded revive on/off |
| `iap_remove_ads_sku` | `remove_ads_299` | which product to sell (for price tests) |
| `review_prompt_min_games` | `5` | earliest point to ask for a rating |

### 3.2 Consent (do this before any ads)

- [ ] Use **MAX's built-in "Terms and Privacy Policy Flow" with Google UMP turned on**:
  - players in the EEA and UK see Google's GDPR consent form
  - everyone else sees a short terms and privacy notice
  - on iOS, Apple's App Tracking Transparency (ATT) prompt comes after that
- [ ] Set up the messages in AdMob, under Privacy & messaging:
  - create and **publish** a GDPR message and a US state privacy message (MAX can't show Google's form until it's published)
  - add every ad network you use to the message's ad partners list
- [ ] Add the iOS text that explains why you ask to track (`NSUserTrackingUsageDescription`).
- [ ] Add a "Privacy Settings" button that reopens the consent form. GDPR regions require it.
- [ ] Pass the consent result to Firebase's consent mode (`ad_storage`, `analytics_storage`, `ad_user_data`, `ad_personalization`) before Analytics starts collecting.
- [ ] Test it:
  - use UMP's debug setting that pretends you're in the EEA
  - try both "Allow" and "Ask App Not to Track" on iOS
  - check each network's consent status in MAX's Mediation Debugger
- [ ] **US app-store age laws:**
  - Texas's law is in force; Utah's and Louisiana's have been delayed.
  - Integrate Apple's Declared Age Range API and Google's Play Age Signals API, following each store's current guidance.
  - Use those age signals only to shape an age-appropriate experience, never for ads or analytics (Google forbids that).
  - Get a short legal check before the US launch.

### 3.3 Ad mediation

- [ ] In MAX, create one app per platform, each with three ad units: banner, interstitial and rewarded.
- [ ] Install the MAX Unity plugin. Through its Integration Manager, add adapters for:
  - Google bidding (AdMob)
  - Unity Ads
  - Mintegral
  - Liftoff Monetize

  With mediation, **each network pays you directly**. Every network needs its own account, payment details and tax form, which is one reason to start with a short list.
- [ ] Platform setup:
  - set the AdMob app IDs
  - add to `Info.plist` the SKAdNetwork IDs that AppLovin lists for your networks (Apple's install-attribution IDs)
  - confirm each SDK ships its Apple privacy manifest
- [ ] Register your own phones as test devices. **Never tap live ads on your own phones**: invalid clicks get AdMob accounts limited.

### 3.4 Where ads appear

- [ ] **`AdsService`** (Unity):
  - wraps MAX
  - preloads interstitial and rewarded ads
  - retries failed loads with growing waits: 2, 4, 8… up to 64 s
- [ ] **`AdPolicy`** (in the engine, unit-tested) decides whether an interstitial may show. It checks:
  - whether the player owns Remove Ads
  - games completed vs. `ads_inter_grace_games`
  - seconds since the last full-screen ad vs. `ads_inter_cooldown_s`
  - whether the player is between games
- [ ] **Interstitial:** shows when the player taps Play Again or Home after a game over, never during a game.
- [ ] **Rewarded revive:**
  - offered once per game on the game-over screen, and hidden if no ad is loaded
  - the reward is a fresh tray guaranteed to fit (small pieces), and the game continues
- [ ] **Banner:** a banner that adapts to the screen width, in the reserved top slot, on the game screen only.

### 3.5 Ad revenue tracking

- [ ] In MAX's `OnAdRevenuePaidEvent` for every ad format, log a Firebase `ad_impression` event with:
  - `ad_platform` = "AppLovin"
  - `ad_source` = the network name
  - `ad_format`
  - `ad_unit_name`
  - `value` = the revenue
  - `currency` = "USD" (MAX reports revenue in US dollars)

  This gives you revenue per player by country and by ad campaign. Later, Google Ads can use it to aim campaigns at revenue instead of installs.

### 3.6 Purchases

- [ ] Use Unity IAP with one permanent (non-consumable) product, `remove_ads_299` at US$2.99. Create the same product ID in App Store Connect and in Play Console.
- [ ] Store ownership on the phone and re-check it at every launch.
- [ ] Add a Restore Purchases button. Apple requires one for non-consumable purchases.
- [ ] Validate purchase receipts on the device with Unity IAP's built-in receipt validation. Add a server check only if you see fraud.
- [ ] Test with Apple sandbox accounts and Google Play license testers. Apple purchases only work once the Paid Apps Agreement is active.

### 3.7 Retention helpers

- [ ] **Rating prompt:**
  - ask after a new high score, once the player has played `review_prompt_min_games` games
  - use Apple's StoreKit review prompt and Google's In-App Review API
  - both stores limit how often the prompt actually appears
- [ ] **Daily reminder:**
  - a notification scheduled on the phone, using Unity Mobile Notifications (no server needed)
  - "Today's puzzle is ready", at most once a day
  - ask for notification permission only after a player finishes their first Daily, not at first launch

**Exit** (both platforms, test ads):
- Consent works, then all three ad formats show.
- `ad_impression` events show up in Firebase DebugView.
- Buying and restoring Remove Ads works.
- Changing a Remote Config value changes the game without a new build.

---

## Phase 4: QA, compliance & store readiness

- [ ] **4.1 Test on several phones:** a low-end Android, a mid-range Android, a small iPhone, a large iPhone and an iPad. Check:
  - interruptions: calls, switching apps, locking the screen
  - the system killing the app mid-game: it comes back in the same position, with the same score
  - airplane mode: the game works, no ads
  - a slow network: ads time out without blocking anything
  - low memory, large font sizes and notches
- [ ] **4.2 Performance and stability targets:**
  - 60 fps, and playable within 3 s of a cold start
  - the prototype's phone checks, moved here from 1.8 so they don't hold up Phase 2 (the [Task 1.8 plan](2026-10-20-task-1.8-unity-prototype.md)'s Results list them): "determinism OK" on IL2CPP ARM64, 0 B GC Alloc while dragging, and the `TraySolver` benchmark, pruned if a deal takes over 4 ms
  - at least 99.5% of users crash-free, measured in Crashlytics
  - below Google Play's warning levels: user-perceived crash rate under 1.09%, ANR rate (app not responding) under 0.47%
- [ ] **4.3 Store listings in English, Brazilian Portuguese, Spanish and Indonesian:**
  - name, subtitle or short description, full description
  - keywords: genre words only, never other games' names
  - icon and a 15–30 s preview video
  - screenshots: 6.9" iPhone, 13" iPad, Android phone and Android tablet
- [ ] **4.4 App Store Connect:**
  - App Privacy answers, including what the ad and analytics SDKs collect (each SDK publishes a guide)
  - the age rating questionnaire
  - the encryption export flag: `ITSAppUsesNonExemptEncryption` = NO
  - EU trader details
  - review notes explaining where the purchase is
- [ ] **4.5 Play Console:**
  - privacy policy
  - "Contains ads" declaration
  - app access
  - content rating (IARC questionnaire)
  - target audience 13+
  - Data safety form
  - advertising ID declaration (the `AD_ID` permission)
  - check the free pre-launch report
- [ ] **4.6 Release builds:**
  - **Android:** an App Bundle signed with your upload key, with Google managing the final signing key (Play App Signing).
  - **iOS:** archived with Xcode 26 and uploaded to TestFlight. External testers need a quick Beta App Review first.
  - **Versioning:** `1.0.0` plus a build number that goes up automatically.
- [ ] **4.7 Automated builds** in GitHub Actions:
  - engine tests run on every push
  - GameCI builds the Android App Bundle and exports the Xcode project
  - a macOS job archives the iOS build and uploads it to TestFlight with fastlane
  - fastlane `supply` uploads Android builds to the internal test track
- [ ] **4.8 Go live in the soft-launch countries.**
  - Personal Play accounts: once the 14-day test is done, apply for production access.
  - Release only in the soft-launch countries chosen in Phase 5.
  - Link both store listings to their AdMob apps. AdMob's readiness review starts only after the app is published and linked, and ads are limited until it passes (usually a couple of days).

**Exit (Gate B):**
- Crash and ANR rates are below target.
- Both stores have approved the app.
- AdMob's readiness review has passed and `app-ads.txt` is verified.

---

## Phase 5: Soft launch (4–8 weeks)

**Goal:** find out with real players whether the game keeps players and earns money, and tune it before spending real money on growth.

- [ ] **5.1 Countries:**
  - **Android:** the Philippines, plus Brazil or Mexico. Installs are cheap and casual audiences are large, which is good for reading retention.
  - **iOS:** Canada or Australia. They're English-speaking with ad prices close to the US, which is good for reading revenue.
  - Keep the US, UK and Germany for the global launch.
- [ ] **5.2 Buy installs:**
  - aim for 1,000–2,000 installs per platform per group of players starting together (a cohort)
  - use Google Ads app campaigns and Apple Ads
  - that's enough to read Day-1 retention within about ±3 points
  - expect US$500–3,000 in total, mostly in the cheap countries
- [ ] **5.3 Weekly dashboard:**
  - retention on days 1, 3, 7, 14 and 30
  - playtime and games per daily player
  - ad impressions per daily player, by format
  - eCPM (revenue per 1,000 ad views), by network and country
  - ARPDAU (average revenue per daily player), by country
  - rewarded-ad opt-in rate
  - Remove Ads conversion rate
  - crash-free users

  Use Firebase plus the MAX dashboard. Add GameAnalytics if you want comparisons with other puzzle games.
- [ ] **5.4 Weekly tuning.**
  - Run Firebase A/B tests (through Remote Config), changing one thing at a time:
    - generator profile
    - interstitial grace period and cooldown
    - banner on or off
    - how the revive is offered
  - Ship a new build every 2 weeks for anything that needs code.
- [ ] **5.5 Gate C: decide.**

| Metric | Rework or stop | Good enough to grow | Strong |
|---|---|---|---|
| Day-1 retention | < 30% | 35–40% | ≥ 45% |
| Day-7 retention | < 10% | 12–15% | ≥ 20% |
| Day-30 retention | < 4% | 5–8% | ≥ 10% |
| Playtime per daily player | < 12 min | 20+ min | 30+ min |
| Crash-free users | < 99% | ≥ 99.5% | ≥ 99.8% |

These are rules of thumb for a block puzzle; check them against GameAnalytics' genre benchmarks. If retention still misses after 2–3 rounds of tuning, rework the twist or stop. Don't try to buy your way out of weak retention.

- [ ] **5.6 Unit economics.** Fill in the model under "Revenue model" with your real numbers. Then decide: pay for growth, grow without paid installs, or rework the game.

---

## Phase 6: Global launch & growth

- [ ] **6.1 Release worldwide** with translated store listings.
  - Keep improving the store pages with A/B tests: Apple's product page optimization and Google Play's store listing experiments (icon, screenshots, the first frame of the video).
  - Watch which search terms bring installs: Play Console's search terms report and Apple Ads keyword data.
- [ ] **6.2 Ask to be featured.**
  - Submit a featuring nomination in App Store Connect.
  - Watch for Google Play indie programs and festivals in your region.
  - Keep a small press kit ready: GIFs, icon, a short description.
- [ ] **6.3 Free social reach.**
  - Post 3–5 short vertical clips a week on TikTok, YouTube Shorts and Instagram Reels: satisfying clears, close saves, Daily results.
  - Keep the in-game share button easy to find.
- [ ] **6.4 Paid installs** (only once Gate D passes):
  - **Attribution.** Before spend goes past about US$1–2K a month, add a tool that tracks which ad brought each install: AppsFlyer, Adjust or Singular. It also handles Apple's privacy-safe attribution (SKAdNetwork / AdAttributionKit).
  - **Google Ads app campaigns.** Start them optimizing for installs. Switch to optimizing for revenue (tROAS) once `ad_impression` revenue is flowing.
  - **Apple Ads.** Use exact-match genre keywords.
  - **Creatives.** Test 3–5 new gameplay videos a week.
  - **Scaling.** Raise budgets in steps of 20–30% while each group's return on ad spend (ROAS) holds.
- [ ] **6.5 Tune how the game earns:**
  - add ad networks one at a time (Pangle, InMobi, DT Exchange, Moloco), A/B testing each in MAX
  - test the Remove Ads price
  - test a Premium subscription: no ads plus exclusive themes. Block Blast sells weekly, monthly and yearly subscriptions in the US at $4.99, $9.99 and $49.99.

**Gate D: pay for installs** in a given country and channel only when:
- the revenue that group of installs is projected to earn over 180 days is at least 20% more than what you paid per install
- its return on ad spend after 7 and 30 days is tracking toward that

---

## Phase 7: Live ops & maintenance

| How often | Work |
|---|---|
| Weekly | KPI review, A/B test results, crash fixes, replies to store reviews, new ad videos |
| Every 2–4 weeks | A release with fixes, one new theme or event, and SDK and adapter updates |
| Quarterly | Ad network review (add or remove networks), price tests, new feature bets |
| Yearly | Raise the Android target API (Google's deadline is Aug 31 each year); meet Apple's new Xcode/iOS SDK requirement (spring); upgrade to the next Unity LTS; review the privacy policy, consent messages and age laws |

Feature backlog, in rough order. Build each one only if the numbers show players want more:
1. Undo, unlocked by watching an ad
2. A themes collection (unlock or buy)
3. Streak rewards
4. Leaderboards (Game Center and Google Play Games)
5. Cloud save
6. A level-based Adventure mode
7. Seasonal events

Never add cash prizes or paid tournaments. Those bring in real-money gaming laws.

---

## Revenue model & unit economics

**Where the money comes from:**
- the rewarded revive (the player chooses to watch; the highest price per view)
- interstitials between games
- the banner during play
- the Remove Ads purchase
- later, possibly a Premium subscription

**The formulas that matter:**
- Revenue per day = daily players × ARPDAU (average revenue per daily player)
- Daily players ≈ new installs per day × average active days per install
- Lifetime value (LTV) per install ≈ ARPDAU × average active days per install
- The most you can pay per install and still break even = LTV

**Worked example.** The numbers are made up to show the math; they're not a forecast.
- Suppose soft launch shows 10 active days per install and a blended ARPDAU of US$0.02.
  - LTV ≈ $0.20, so paid installs only make sense where they cost well under $0.20.
  - Holding 10,000 daily players takes about 1,000 new installs a day.
  - That earns about $200 a day, roughly $6,000 a month before tax.
- In a market like Canada, ARPDAU might be $0.06. LTV is then about $0.60, so you can afford pricier installs there.

**Reality check:**
- Ad revenue per player varies enormously by country. A US player can be worth ten times an Indonesian one.
- Block Blast's global average of about one cent per player per day comes from huge volume in low-price markets.
- Retention is the lever. Every extra active day per install raises LTV directly.

**When the money arrives:**

| Source | Paid |
|---|---|
| App Store purchases | Monthly, within 45 days after the end of Apple's fiscal month. Apple keeps 15% under its Small Business Program |
| Google Play purchases | Monthly. Google keeps 15% of your first US$1M each year |
| AdMob / Google bidding | Monthly, around the 21st, once your balance reaches US$100 |
| AppLovin and each other ad network | Monthly and separately, each with its own minimum payout and a 30–60 day delay |

## Budget (cash, not counting your time)

| Item | Cost (US$) |
|---|---|
| Apple Developer Program | 99 a year |
| Google Play Console | 25 once |
| Domain + static website | ~15 a year (GitHub Pages hosting is free) |
| Mac for iOS builds | 0 if you have one; ~600 for a Mac mini; or a cloud Mac / CI by the hour |
| Test phones (low-end Android + used iPhone) | 200–400 |
| Art, UI and icon | 100–1,500 for asset packs; 1,500–4,000 for custom art |
| Sound effects and music | 0–300 |
| Unity Personal, Firebase free tier, MAX, AdMob, GameCI | 0 |
| Soft-launch installs | 500–3,000 |
| Trademark registration (optional) | a few hundred per country and class |
| **Total to soft launch** | **about 1,000–5,000**, plus a Mac if you need one |

## Risks

| Risk | What to do about it |
|---|---|
| Apple rejects it as a copycat or spam (guidelines 4.1 / 4.3) | Original name, art and twist (the Daily Puzzle); real polish; no other games' names in the store text |
| AdMob limits ad serving | Publish and link the app, verify `app-ads.txt`, register test devices, never click your own ads |
| Players don't stick around | A generator tuned with the simulator, repeated tutorial tests, and Gate C before any spending |
| Installs cost more than players earn | No scaling before Gate D; free channels and cheap markets first |
| Privacy, consent or age rules change | A yearly compliance check; MAX + UMP's maintained consent flow; a legal review before the US and EU launches |
| Copyright claim or takedown | Original assets with licences on file. Block Blast itself was pulled from Google Play for about a day in May 2026 |
| iOS builds are awkward from Windows | Arrange a Mac or cloud builds in Phase 0, and prove device builds in week 1 |
| Scope creep or solo burnout | Only Classic + Daily until Gate C passes |

## References

- Google Play testing requirements for new personal accounts: https://support.google.com/googleplay/android-developer/answer/14151465
- Google Play target API requirements: https://developer.android.com/google/play/requirements/target-sdk
- Apple SDK minimum requirements: https://developer.apple.com/news/upcoming-requirements/
- App Store Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- AppLovin MAX Terms and Privacy Policy Flow (Unity): https://developers.applovin.com/en/max/unity/overview/terms-and-privacy-policy-flow
- AdMob app readiness review: https://support.google.com/admob/answer/10564477
- Firebase ad revenue measurement: https://firebase.google.com/docs/analytics/measure-ad-revenue
- Google Play Age Signals: https://support.google.com/googleplay/android-developer/answer/16569691
- Apple EU Digital Services Act trader requirements: https://developer.apple.com/help/app-store-connect/manage-compliance-information/manage-eu-digital-services-act-compliance-information
- Unity 6 releases and support: https://unity.com/releases/unity-6
- Unity Personal: https://unity.com/products/unity-personal
