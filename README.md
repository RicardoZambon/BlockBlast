# Block puzzle game

An original 8×8 block puzzle for iOS and Android (working title). This repository holds the plans, the game's rules engine and the Unity app: Home, a first-time tutorial, Classic with pause and game over, the Daily Puzzle, and Settings.

- [Master plan](docs/superpowers/plans/2026-10-15-block-puzzle-game.md): every phase from setup to revenue
- [Phase 1 plan](docs/superpowers/plans/2026-10-20-phase-1-engine-simulator.md): the engine and the simulator, step by step
- [Task 1.8 plan](docs/superpowers/plans/2026-10-20-task-1.8-unity-prototype.md): the rough Unity prototype, step by step
- [Phase 2a plan](docs/superpowers/plans/2026-10-20-phase-2a-app-shell.md): the app shell, with its screens and a safe, banner-aware layout
- [Phase 2b plan](docs/superpowers/plans/2026-10-20-phase-2b-save-and-resume.md): pausing on interruptions, and a backup save
- [Phase 2c plan](docs/superpowers/plans/2026-10-20-phase-2c-daily-puzzle.md): the Daily Puzzle, with its streak and Share
- [Phase 2d plan](docs/superpowers/plans/2026-10-20-phase-2d-tutorial.md): the first-time tutorial

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
