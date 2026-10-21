# Block puzzle game

An original 8×8 block puzzle for iOS and Android (working title). This repository holds the plans, the game's rules engine and the Unity app: Home, a first-time tutorial, Classic with pause and game over, the Daily Puzzle, and Settings.

- [Design](docs/design.md): the game on one page: rules, pieces, scoring, modes, ad rules, art direction and KPIs
- [Master plan](docs/superpowers/plans/2026-10-15-block-puzzle-game.md): every phase from setup to revenue
- [Phase 1 plan](docs/superpowers/plans/2026-10-20-phase-1-engine-simulator.md): the engine and the simulator, step by step
- [Task 1.8 plan](docs/superpowers/plans/2026-10-20-task-1.8-unity-prototype.md): the rough Unity prototype, step by step
- [Phase 2a plan](docs/superpowers/plans/2026-10-20-phase-2a-app-shell.md): the app shell, with its screens and a safe, banner-aware layout
- [Phase 2b plan](docs/superpowers/plans/2026-10-20-phase-2b-save-and-resume.md): pausing on interruptions, and a backup save
- [Phase 2c plan](docs/superpowers/plans/2026-10-20-phase-2c-daily-puzzle.md): the Daily Puzzle, with its streak and Share
- [Phase 2d plan](docs/superpowers/plans/2026-10-20-phase-2d-tutorial.md): the first-time tutorial
- [Phase 2e plan](docs/superpowers/plans/2026-10-20-phase-2e-feedback.md): visual, sound and haptic feedback
- [Phase 2f plan](docs/superpowers/plans/2026-10-20-phase-2f-accessibility.md): contrast, brightness and text-size checks
- [Phase 2g plan](docs/superpowers/plans/2026-10-21-phase-2g-strings.md): every UI string in one table, the first step of translations
- [Phase 2h plan](docs/superpowers/plans/2026-10-21-phase-2h-languages.md): Brazilian Portuguese, Latin-American Spanish and Indonesian
- [Phase 3a plan](docs/superpowers/plans/2026-10-21-phase-3a-ad-placements.md): ad placements behind a consent gate, with stand-in services
- [Phase 3b plan](docs/superpowers/plans/2026-10-21-phase-3b-retention.md): the rating prompt and the Daily reminder
- [Phase 3c plan](docs/superpowers/plans/2026-10-21-phase-3c-analytics.md): analytics events behind consent
- [Phase 3d plan](docs/superpowers/plans/2026-10-21-phase-3d-purchases.md): Remove Ads and Restore Purchases
- [Phase 3e plan](docs/superpowers/plans/2026-10-21-phase-3e-ad-revenue.md): an `ad_impression` event for every paid ad
- [Phase 3f plan](docs/superpowers/plans/2026-10-21-phase-3f-consent.md): consent results for analytics, and Privacy Settings
- [Phase 3g plan](docs/superpowers/plans/2026-10-21-phase-3g-remote-config.md): Remote Config's tuning keys, with bundled defaults
- [Phase 4a plan](docs/superpowers/plans/2026-10-21-phase-4a-automated-builds.md): Unity tests and builds in GitHub Actions, once the licence secrets exist

## Layout

| Path | What it is |
|---|---|
| `core/src/` | The rules engine: plain C# 9 with no Unity references. Also a local Unity package (`package.json`, `.asmdef`) |
| `core/dotnet/BlockPuzzle.Core/` | .NET project that compiles `core/src` (netstandard2.1) |
| `core/dotnet/BlockPuzzle.Core.Tests/` | xUnit tests for the engine |
| `core/dotnet/BlockPuzzle.Sim/` | Simulator: bots play thousands of games to tune the piece generator |
| `core/dotnet/BlockPuzzle.Sim.Tests/` | xUnit tests for the simulator |
| `unity/` | The Unity 6.3 LTS app (6000.3.26f1, Universal 2D). The game is in `unity/Assets/Game` |
| `unity/Assets/Game/Tests/` | Unity EditMode and PlayMode tests |

## Run

```bash
dotnet test core/BlockPuzzle.slnx
dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 20000
dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 20000 --profile baseline --check-target
```

CI runs the tests and the last command, which exits with code 2 if the greedy bot on the baseline profile misses the Phase 1 difficulty target: a median of 80–150 placements, with under 5% of games ending before placement 20.

### Unity

Open `unity/` in Unity 6000.3.26f1, open `Assets/Game/Scenes/Game.unity` and press Play. Besides the camera and a 2D light, the scene holds one object, `App`, which builds Home, the game and Settings in code. To run the tests from a shell:
- Close the Editor, because it locks the project.
- Keep Unity Hub running: the Editor gets its license from the Hub's licensing client.

```bash
export UNITY="/c/Program Files/Unity/Hub/Editor/6000.3.26f1/Editor/Unity.exe"
"$UNITY" -batchmode -nographics -projectPath unity -runTests -testPlatform EditMode -testResults "$PWD/unity/Logs/editmode.xml" -logFile "$PWD/unity/Logs/editmode.log"
"$UNITY" -batchmode -nographics -projectPath unity -runTests -testPlatform PlayMode -testResults "$PWD/unity/Logs/playmode.xml" -logFile "$PWD/unity/Logs/playmode.log"
```

The menu **Block Puzzle** (or `-executeMethod BlockPuzzle.Game.EditorTools.ProjectSetup.<method>`) offers three methods:
- `ApplySettings` reapplies the project settings.
- `BuildScene` regenerates the scene.
- `BuildAndroidDevelopment` builds `unity/Builds/Android/blockpuzzle-dev.apk`. Add `-buildTarget Android` when running it from a shell.

### Android emulator

An emulator can check everything except timings, without a phone:
- **What runs:** the Android 17 image (`system-images;android-37.0;google_apis;x86_64`) runs the ARM64 development APK through Google's ARM translation. So timings there don't count, but determinism, allocations, resume and layout do.
- **What doesn't:**
  - The Android 14 image's translator stops at an instruction Unity's player uses.
  - Unity 6.3 no longer builds x86_64 for Android.

```bash
"$LOCALAPPDATA/Android/Sdk/emulator/emulator.exe" -avd <name> -no-window -no-audio -no-boot-anim -gpu host
"$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe" install -r unity/Builds/Android/blockpuzzle-dev.apk
```

A Unity build restarts `adb`. If the emulator then shows as offline, run `adb kill-server` and connect again. The [Task 1.8 plan](docs/superpowers/plans/2026-10-20-task-1.8-unity-prototype.md)'s Results have the first run.

## Automated builds

GitHub Actions runs two workflows:
- **`core.yml`** (the `test` check that `main` requires) runs the engine tests and the difficulty target on every push.
- **`unity.yml`** uses GameCI:
  - Unity's EditMode and PlayMode tests on every pull request and push to `main`.
  - An Android App Bundle and an exported Xcode project on `main` and on demand (Actions → unity → Run workflow), kept as run artifacts.

`unity.yml` needs a Unity licence, so its jobs are skipped until the repository has these secrets (Settings → Secrets and variables → Actions). Never commit any of them.

| Secret | What it is |
|---|---|
| `UNITY_LICENSE` | The whole contents of `Unity_lic.ulf`. In Unity Hub: Preferences → Licenses → Add → Get a free personal license. The file is then at `C:\ProgramData\Unity\Unity_lic.ulf`. If it isn't, [GameCI's activation guide](https://game.ci/docs/github/activation) has other ways |
| `UNITY_EMAIL` | Your Unity account's email |
| `UNITY_PASSWORD` | Your Unity account's password |

With your upload key (master plan 4.6), four more sign the bundle for Google Play: `ANDROID_KEYSTORE_BASE64` (the keystore file, base64-encoded), `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` and `ANDROID_KEYALIAS_PASS`. Without them, Unity's debug key signs it, which Play doesn't accept.
