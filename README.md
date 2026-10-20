# Block puzzle game

An original 8×8 block puzzle for iOS and Android (working title). This repository holds the plans and the game's rules engine; the Unity app comes next.

- [Master plan](docs/superpowers/plans/2026-10-15-block-puzzle-game.md): every phase from setup to revenue
- [Phase 1 plan](docs/superpowers/plans/2026-10-20-phase-1-engine-simulator.md): the engine and the simulator, step by step

## Layout

| Path | What it is |
|---|---|
| `core/src/` | The rules engine: plain C# 9 with no Unity references. Also a local Unity package (`package.json`, `.asmdef`) |
| `core/dotnet/BlockPuzzle.Core/` | .NET project that compiles `core/src` (netstandard2.1) |
| `core/dotnet/BlockPuzzle.Core.Tests/` | xUnit tests |
| `core/dotnet/BlockPuzzle.Sim/` | Simulator: bots play thousands of games to tune the piece generator |

## Run

```bash
dotnet test core/BlockPuzzle.slnx
dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 20000
```
