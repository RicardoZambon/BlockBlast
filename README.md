# Block puzzle game

An original 8×8 block puzzle for iOS and Android (working title). This repository holds the plans and the game's rules engine; the Unity app comes next.

- [Master plan](docs/superpowers/plans/2026-10-15-block-puzzle-game.md): every phase from setup to revenue
- [Phase 1 plan](docs/superpowers/plans/2026-10-20-phase-1-engine-simulator.md): the engine and the simulator, step by step
- [Task 1.8 plan](docs/superpowers/plans/2026-10-20-task-1.8-unity-prototype.md): the rough Unity prototype, step by step

## Layout

| Path | What it is |
|---|---|
| `core/src/` | The rules engine: plain C# 9 with no Unity references. Also a local Unity package (`package.json`, `.asmdef`) |
| `core/dotnet/BlockPuzzle.Core/` | .NET project that compiles `core/src` (netstandard2.1) |
| `core/dotnet/BlockPuzzle.Core.Tests/` | xUnit tests for the engine |
| `core/dotnet/BlockPuzzle.Sim/` | Simulator: bots play thousands of games to tune the piece generator |
| `core/dotnet/BlockPuzzle.Sim.Tests/` | xUnit tests for the simulator |

## Run

```bash
dotnet test core/BlockPuzzle.slnx
dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 20000
dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 20000 --profile baseline --check-target
```

CI runs the tests and the last command, which exits with code 2 if the greedy bot on the baseline profile misses the Phase 1 difficulty target: a median of 80–150 placements, with under 5% of games ending before placement 20.
