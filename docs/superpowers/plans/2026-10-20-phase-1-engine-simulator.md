# Phase 1: Engine & Simulator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A tested, deterministic C# rules engine for the 8×8 block puzzle, plus a .NET simulator. The simulator has bots play thousands of games to tune how the piece generator deals pieces for each difficulty profile.

**Architecture:**
- The engine lives in `core/src` as plain C# 9 with no Unity or NuGet dependencies, so the same files compile in Unity 6 and in .NET.
- A `netstandard2.1` project compiles those files for the xUnit tests and the simulator.
- The board is one 64-bit integer, and every piece pre-computes a mask for each position it can take, so the simulator can play tens of thousands of games in seconds.
- All randomness goes through a seeded PCG32 generator using integer math only. A game can therefore be replayed or resumed exactly on any platform, which the Daily Puzzle needs.

**Tech Stack:**
- Engine: C# 9 on `netstandard2.1`
- Tests: .NET 10 + xUnit 2.9.3
- Simulator: .NET 10 console app
- CI: GitHub Actions

This plan covers master-plan tasks 1.1–1.7 ([master plan](2026-10-15-block-puzzle-game.md)). Tasks 1.8 (the Unity prototype) and 1.9 (playtests) need Unity and real people, so they get their own plan.

## Global Constraints

- **C# 9 only in the engine (`core/src`).** That's Unity 6's compiler level. Not allowed: file-scoped namespaces, `record`s, global usings, `init` accessors. `<LangVersion>9.0</LangVersion>` enforces this.
- **No dependencies in the engine:** no `UnityEngine` references and no NuGet packages. Every engine file starts with `#nullable enable`.
- **Deterministic:**
  - All randomness comes from `Pcg32`.
  - Piece dealing uses integer weights only: no `System.Random`, no floating-point probabilities.
- **Unity-friendly data classes:** settings and snapshot classes use public fields and `[Serializable]`, so Unity's `JsonUtility` and the Remote Config loader can fill them.
- **Warnings are errors** in the engine and simulator projects.
- **Board rules:**
  - 8×8, bit index = `row * 8 + col`, row 0 at the top.
  - Full rows and full columns clear at the same time.
  - Pieces never rotate.
- **Defaults from the master plan:**
  - 1 point per cell placed.
  - Clearing `n` lines at once scores `10 × n(n+1)/2`.
  - Each extra streak level adds 50% to line points; the streak resets after 3 placements without a clear.
  - +300 for clearing the whole board.
  - Ads: no interstitials during the first 3 games; at least 90 s between full-screen ads.
- **Simulator target for the baseline profile + greedy bot:**
  - median of 80–150 placements per game
  - under 5% of games ending before placement 20

## File Structure

| Path | Responsibility |
|---|---|
| `.gitignore`, `.gitattributes` | Ignore .NET/Unity build output; normalize line endings |
| `README.md` | What the repo is, layout, how to run tests and the simulator |
| `.github/workflows/core.yml` | CI: run the tests and check the simulator's difficulty target |
| `core/BlockPuzzle.slnx` | Solution with the four .NET projects |
| `core/src/package.json`, `core/src/BlockPuzzle.Core.asmdef` | Makes `core/src` a local Unity package with no engine references |
| `core/src/Board.cs` | 64-bit board: fits, place, simultaneous row/column clears, text parsing |
| `core/src/PlaceResult.cs` | Result of a placement: new board, lines, cleared cells |
| `core/src/PieceCategory.cs` | Size groups: Tiny, Small, Medium, Large |
| `core/src/PieceShape.cs` | A shape built from text art + its precomputed placement masks |
| `core/src/PieceCatalog.cs` | The standard 37-shape set and lookup by id |
| `core/src/Pcg32.cs` | Seeded, platform-independent random generator with save/restore |
| `core/src/ScoringSettings.cs`, `core/src/Scoring.cs` | Score values and the pure scoring function |
| `core/src/FitGuarantee.cs`, `core/src/DifficultyBand.cs`, `core/src/GeneratorSettings.cs` | Generator tuning values and the gentle/baseline/hard presets |
| `core/src/TraySolver.cs` | Whether a set of pieces can all be placed in some order |
| `core/src/PieceGenerator.cs` | Deals trays of three: weights, helpful bias, mercy, fit guarantees, rescue tray |
| `core/src/GameSnapshot.cs`, `core/src/GameSession.cs` | One game: tray, placements, score, game over, revive, save/resume |
| `core/src/AdPolicySettings.cs`, `core/src/AdPolicy.cs` | When an interstitial may show |
| `core/dotnet/BlockPuzzle.Core/BlockPuzzle.Core.csproj` | Compiles `../../src/**/*.cs` for .NET |
| `core/dotnet/BlockPuzzle.Core.Tests/*` | xUnit tests, one file per engine area |
| `core/dotnet/BlockPuzzle.Sim/*` | Simulator: options, bots, runner, report, target check |
| `core/dotnet/BlockPuzzle.Sim.Tests/*` | xUnit tests for the simulator |

Commit messages end with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

---

### Task 1: Project setup + Board

**Files:**
- Create: `.gitignore`, `.gitattributes`, `README.md`, `.github/workflows/core.yml`
- Create: `core/src/package.json`, `core/src/BlockPuzzle.Core.asmdef`
- Create: `core/dotnet/BlockPuzzle.Core/BlockPuzzle.Core.csproj`, `core/dotnet/BlockPuzzle.Core.Tests/BlockPuzzle.Core.Tests.csproj`, `core/BlockPuzzle.slnx`
- Create: `core/src/Board.cs`, `core/src/PlaceResult.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/TestBoards.cs`, `core/dotnet/BlockPuzzle.Core.Tests/BoardTests.cs`

**Interfaces:**
- Produces:
  - `readonly struct Board`, with:
    - `const int Size = 8`, `ulong Cells`, `Board(ulong)`
    - `static Board Empty`, `bool IsEmpty`
    - `static int BitIndex(int row, int col)`, `bool IsFilled(int row, int col)`
    - `bool Fits(ulong mask)`, `PlaceResult Place(ulong mask)` (throws `InvalidOperationException` on overlap)
    - `static Board Parse(params string[] rows)`, `ToString()`
    - value equality
  - `readonly struct PlaceResult`, with `Board Board`, `int Lines`, `ulong ClearedMask`, `bool BoardCleared`
  - Test helpers: `TestBoards.Checkerboard`, `TestBoards.Cell(row, col)`

- [ ] **Step 1: Create the repository files**

`.gitignore`:
```gitignore
# .NET
bin/
obj/
TestResults/
*.user
*.suo
.vs/
.idea/

# Unity project (created in master-plan task 1.8)
unity/[Ll]ibrary/
unity/[Tt]emp/
unity/[Oo]bj/
unity/[Bb]uild/
unity/[Bb]uilds/
unity/[Ll]ogs/
unity/[Uu]ser[Ss]ettings/
unity/[Mm]emoryCaptures/
unity/*.csproj
unity/*.sln
unity/*.slnx

# OS
.DS_Store
Thumbs.db
```

`.gitattributes`:
```gitattributes
* text=auto
```

`README.md`:
````markdown
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
````

`.github/workflows/core.yml`:
```yaml
name: core

on:
  push:
  pull_request:

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Test the engine
        run: dotnet test core/BlockPuzzle.slnx -c Release
```

`core/src/package.json` (`yourstudio` gets replaced when the studio name is chosen, master-plan task 0.2):
```json
{
  "name": "com.yourstudio.blockpuzzle.core",
  "version": "0.1.0",
  "displayName": "Block Puzzle Core",
  "description": "Pure C# rules engine for the block puzzle game.",
  "unity": "6000.3"
}
```

`core/src/BlockPuzzle.Core.asmdef`:
```json
{
  "name": "BlockPuzzle.Core",
  "rootNamespace": "BlockPuzzle.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": true
}
```

`core/dotnet/BlockPuzzle.Core/BlockPuzzle.Core.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <!-- Unity 6 compiles C# 9; keep the engine at that level so the same files build in both places. -->
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>BlockPuzzle.Core</RootNamespace>
    <AssemblyName>BlockPuzzle.Core</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <!-- The sources live in core/src, which is also the Unity package folder. -->
    <Compile Include="../../src/**/*.cs" Link="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

</Project>
```

`core/dotnet/BlockPuzzle.Core.Tests/BlockPuzzle.Core.Tests.csproj` (package versions from the .NET 10 SDK's xUnit template):
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="BlockPuzzle.Core" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BlockPuzzle.Core\BlockPuzzle.Core.csproj" />
  </ItemGroup>

</Project>
```

Create the solution:
```bash
dotnet new sln -n BlockPuzzle -o core
dotnet sln core/BlockPuzzle.slnx add core/dotnet/BlockPuzzle.Core/BlockPuzzle.Core.csproj core/dotnet/BlockPuzzle.Core.Tests/BlockPuzzle.Core.Tests.csproj
```
Expected: `core/BlockPuzzle.slnx` is created (the .NET 10 default format) and both projects are added.

- [ ] **Step 2: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/TestBoards.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

static class TestBoards
{
    /// <summary>No two empty cells touch, so only the single-cell piece fits.</summary>
    public static readonly Board Checkerboard = Board.Parse(
        "#.#.#.#.",
        ".#.#.#.#",
        "#.#.#.#.",
        ".#.#.#.#",
        "#.#.#.#.",
        ".#.#.#.#",
        "#.#.#.#.",
        ".#.#.#.#");

    public static ulong Cell(int row, int col) => 1UL << Board.BitIndex(row, col);
}
```

`core/dotnet/BlockPuzzle.Core.Tests/BoardTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class BoardTests
{
    [Fact]
    public void Parse_and_ToString_round_trip()
    {
        string[] rows =
        {
            "#.......",
            ".#......",
            "..#.....",
            "...#....",
            "....#...",
            ".....#..",
            "......#.",
            ".......#",
        };

        var board = Board.Parse(rows);

        Assert.Equal(string.Join("\n", rows), board.ToString());
        Assert.True(board.IsFilled(0, 0));
        Assert.False(board.IsFilled(0, 1));
        Assert.True(board.IsFilled(7, 7));
    }

    [Fact]
    public void Parse_rejects_bad_input()
    {
        Assert.Throws<ArgumentException>(() => Board.Parse("........"));
        Assert.Throws<ArgumentException>(() => Board.Parse(
            "........", "........", "........", "........", "........", "........", "........", "......."));
        Assert.Throws<ArgumentException>(() => Board.Parse(
            "x.......", "........", "........", "........", "........", "........", "........", "........"));
    }

    [Fact]
    public void Fits_is_false_only_where_cells_overlap()
    {
        var board = new Board(TestBoards.Cell(0, 0));

        Assert.False(board.Fits(TestBoards.Cell(0, 0)));
        Assert.True(board.Fits(TestBoards.Cell(0, 1)));
    }

    [Fact]
    public void Placing_without_full_lines_only_adds_cells()
    {
        ulong piece = TestBoards.Cell(0, 0) | TestBoards.Cell(0, 1);

        var result = Board.Empty.Place(piece);

        Assert.Equal(piece, result.Board.Cells);
        Assert.Equal(0, result.Lines);
        Assert.Equal(0UL, result.ClearedMask);
        Assert.False(result.BoardCleared);
    }

    [Fact]
    public void Placing_on_filled_cells_throws()
    {
        var board = new Board(TestBoards.Cell(3, 3));

        Assert.Throws<InvalidOperationException>(() => board.Place(TestBoards.Cell(3, 3)));
    }

    [Fact]
    public void Completing_a_row_clears_it()
    {
        var board = Board.Parse(
            "#######.",
            "#.......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");

        var result = board.Place(TestBoards.Cell(0, 7));

        Assert.Equal(1, result.Lines);
        Assert.Equal(0xFFUL, result.ClearedMask);
        Assert.Equal(new Board(TestBoards.Cell(1, 0)), result.Board);
        Assert.False(result.BoardCleared);
    }

    [Fact]
    public void Completing_a_column_clears_it()
    {
        var board = Board.Parse(
            "...#....",
            "...#....",
            "...#....",
            "........",
            "...#....",
            "...#....",
            "...#...#",
            "...#....");

        var result = board.Place(TestBoards.Cell(3, 3));

        Assert.Equal(1, result.Lines);
        Assert.Equal(0x0101010101010101UL << 3, result.ClearedMask);
        Assert.Equal(new Board(TestBoards.Cell(6, 7)), result.Board);
    }

    [Fact]
    public void Completing_a_row_and_a_column_at_once_clears_both()
    {
        var board = Board.Parse(
            ".....#..",
            ".....#..",
            "#####.##",
            ".....#..",
            ".....#..",
            ".....#..",
            ".....#..",
            "#....#..");

        var result = board.Place(TestBoards.Cell(2, 5));

        Assert.Equal(2, result.Lines);
        Assert.Equal(new Board(TestBoards.Cell(7, 0)), result.Board);
    }

    [Fact]
    public void Clearing_every_filled_cell_reports_a_board_clear()
    {
        var board = Board.Parse(
            "........",
            "........",
            "........",
            "#######.",
            "........",
            "........",
            "........",
            "........");

        var result = board.Place(TestBoards.Cell(3, 7));

        Assert.True(result.Board.IsEmpty);
        Assert.True(result.BoardCleared);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'Board' could not be found`.

- [ ] **Step 4: Implement `Board` and `PlaceResult`**

`core/src/PlaceResult.cs`:
```csharp
#nullable enable
namespace BlockPuzzle.Core
{
    /// <summary>What happened when a piece was placed.</summary>
    public readonly struct PlaceResult
    {
        public readonly Board Board;
        public readonly int Lines;
        /// <summary>The cells that were cleared, so the view can animate exactly those.</summary>
        public readonly ulong ClearedMask;

        public PlaceResult(Board board, int lines, ulong clearedMask)
        {
            Board = board;
            Lines = lines;
            ClearedMask = clearedMask;
        }

        public bool BoardCleared => Lines > 0 && Board.IsEmpty;
    }
}
```

`core/src/Board.cs`:
```csharp
#nullable enable
using System;
using System.Text;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// The 8x8 board, stored in one 64-bit integer. Bit index = row * 8 + col; row 0 is the top row.
    /// </summary>
    public readonly struct Board : IEquatable<Board>
    {
        public const int Size = 8;

        // 8 row masks followed by 8 column masks.
        static readonly ulong[] LineMasks = BuildLineMasks();

        public readonly ulong Cells;

        public Board(ulong cells) { Cells = cells; }

        public static Board Empty => default;

        public bool IsEmpty => Cells == 0;

        public static int BitIndex(int row, int col) => row * Size + col;

        public bool IsFilled(int row, int col)
        {
            if ((uint)row >= Size) throw new ArgumentOutOfRangeException(nameof(row));
            if ((uint)col >= Size) throw new ArgumentOutOfRangeException(nameof(col));
            return (Cells & (1UL << BitIndex(row, col))) != 0;
        }

        public bool Fits(ulong pieceMask) => (Cells & pieceMask) == 0;

        /// <summary>Adds the piece, then clears every full row and column at the same time.</summary>
        public PlaceResult Place(ulong pieceMask)
        {
            if (!Fits(pieceMask)) throw new InvalidOperationException("The piece overlaps filled cells.");

            ulong filled = Cells | pieceMask;
            ulong cleared = 0;
            int lines = 0;
            foreach (ulong line in LineMasks)
            {
                if ((filled & line) == line)
                {
                    cleared |= line;
                    lines++;
                }
            }
            return new PlaceResult(new Board(filled & ~cleared), lines, cleared);
        }

        /// <summary>Builds a board from 8 rows of 8 characters: '#' = filled, '.' = empty.</summary>
        public static Board Parse(params string[] rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (rows.Length != Size) throw new ArgumentException($"Expected {Size} rows, got {rows.Length}.", nameof(rows));

            ulong cells = 0;
            for (int r = 0; r < Size; r++)
            {
                string row = rows[r];
                if (row == null || row.Length != Size) throw new ArgumentException($"Row {r} must have {Size} characters.", nameof(rows));
                for (int c = 0; c < Size; c++)
                {
                    char ch = row[c];
                    if (ch == '#') cells |= 1UL << BitIndex(r, c);
                    else if (ch != '.') throw new ArgumentException($"Unexpected character '{ch}' at row {r}, column {c}.", nameof(rows));
                }
            }
            return new Board(cells);
        }

        public override string ToString()
        {
            var text = new StringBuilder(Size * (Size + 1));
            for (int r = 0; r < Size; r++)
            {
                if (r > 0) text.Append('\n');
                for (int c = 0; c < Size; c++) text.Append(IsFilled(r, c) ? '#' : '.');
            }
            return text.ToString();
        }

        public bool Equals(Board other) => Cells == other.Cells;
        public override bool Equals(object? obj) => obj is Board other && Equals(other);
        public override int GetHashCode() => Cells.GetHashCode();
        public static bool operator ==(Board left, Board right) => left.Equals(right);
        public static bool operator !=(Board left, Board right) => !left.Equals(right);

        static ulong[] BuildLineMasks()
        {
            var masks = new ulong[2 * Size];
            for (int i = 0; i < Size; i++)
            {
                masks[i] = 0xFFUL << (i * Size);              // row i
                masks[Size + i] = 0x0101010101010101UL << i;  // column i
            }
            return masks;
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 9`.

- [ ] **Step 6: Commit**

```bash
git add .gitignore .gitattributes README.md .github core docs
git commit -m "Add engine solution and the 64-bit board" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Piece shapes and the standard catalog

**Files:**
- Create: `core/src/PieceCategory.cs`, `core/src/PieceShape.cs`, `core/src/PieceCatalog.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/PieceShapeTests.cs`, `core/dotnet/BlockPuzzle.Core.Tests/PieceCatalogTests.cs`

**Interfaces:**
- Consumes: `Board`, `Board.BitIndex`, `Board.Size`, `Board.Fits`, `Board.Place`
- Produces:
  - `enum PieceCategory { Tiny, Small, Medium, Large }`
  - `sealed class PieceShape`, with:
    - `string Id`, `PieceCategory Category`, `int Width`, `int Height`, `int CellCount`, `ulong BaseMask`
    - `ReadOnlySpan<ulong> Placements`, `int PlacementCount`, `int AnchorRow(int)`, `int AnchorCol(int)`
    - `bool TryGetMask(int row, int col, out ulong mask)`, `bool FitsAnywhere(Board)`, `bool CanClearLine(Board)`
    - `static PieceShape FromRows(string id, PieceCategory category, params string[] rows)`
  - `sealed class PieceCatalog`, with:
    - `PieceCatalog(IEnumerable<PieceShape>)`, `static PieceCatalog Standard`
    - `IReadOnlyList<PieceShape> Shapes`, indexer `this[string id]`, `bool TryGet(string id, out PieceShape? shape)`

- [ ] **Step 1: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/PieceShapeTests.cs`:
```csharp
using System.Numerics;

namespace BlockPuzzle.Core.Tests;

public class PieceShapeTests
{
    [Fact]
    public void FromRows_reads_size_and_cells()
    {
        var shape = PieceShape.FromRows("l4a", PieceCategory.Medium, "#.", "#.", "##");

        Assert.Equal(2, shape.Width);
        Assert.Equal(3, shape.Height);
        Assert.Equal(4, shape.CellCount);
        Assert.Equal(
            TestBoards.Cell(0, 0) | TestBoards.Cell(1, 0) | TestBoards.Cell(2, 0) | TestBoards.Cell(2, 1),
            shape.BaseMask);
    }

    [Theory]
    [InlineData(new[] { "#" }, 64)]
    [InlineData(new[] { "###", "###", "###" }, 36)]
    [InlineData(new[] { "#####" }, 32)]
    [InlineData(new[] { "#", "#", "#", "#", "#" }, 32)]
    public void Placement_count_is_every_position_inside_the_board(string[] rows, int expected)
    {
        var shape = PieceShape.FromRows("x", PieceCategory.Small, rows);

        Assert.Equal(expected, shape.PlacementCount);
    }

    [Fact]
    public void Each_placement_is_the_shape_moved_to_its_anchor()
    {
        var shape = PieceShape.FromRows("t4a", PieceCategory.Medium, "###", ".#.");

        for (int i = 0; i < shape.PlacementCount; i++)
        {
            int row = shape.AnchorRow(i);
            int col = shape.AnchorCol(i);
            Assert.True(row + shape.Height <= Board.Size);
            Assert.True(col + shape.Width <= Board.Size);
            Assert.True(shape.TryGetMask(row, col, out ulong mask));
            Assert.Equal(mask, shape.Placements[i]);
            Assert.Equal(shape.CellCount, BitOperations.PopCount(mask));
        }
    }

    [Fact]
    public void TryGetMask_rejects_positions_that_leave_the_board()
    {
        var shape = PieceShape.FromRows("i3h", PieceCategory.Small, "###");

        Assert.True(shape.TryGetMask(0, 5, out _));
        Assert.False(shape.TryGetMask(0, 6, out _));
        Assert.False(shape.TryGetMask(-1, 0, out _));
        Assert.False(shape.TryGetMask(8, 0, out _));
    }

    [Fact]
    public void FromRows_rejects_bad_art()
    {
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("a", PieceCategory.Small));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("b", PieceCategory.Small, "##", "#"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("c", PieceCategory.Small, "..", "##"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("d", PieceCategory.Small, "#.", "#."));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("e", PieceCategory.Small, "#x"));
        Assert.Throws<ArgumentException>(() => PieceShape.FromRows("", PieceCategory.Small, "#"));
    }

    [Fact]
    public void FitsAnywhere_and_CanClearLine_check_every_position()
    {
        var board = Board.Parse(
            "######..",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");
        var domino = PieceShape.FromRows("i2h", PieceCategory.Small, "##");
        var tromino = PieceShape.FromRows("i3h", PieceCategory.Small, "###");

        Assert.True(domino.FitsAnywhere(board));
        Assert.True(domino.CanClearLine(board));
        Assert.False(tromino.CanClearLine(board));
        Assert.False(domino.FitsAnywhere(TestBoards.Checkerboard));
    }
}
```

`core/dotnet/BlockPuzzle.Core.Tests/PieceCatalogTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class PieceCatalogTests
{
    [Fact]
    public void The_standard_catalog_has_37_unique_shapes()
    {
        var shapes = PieceCatalog.Standard.Shapes;

        Assert.Equal(37, shapes.Count);
        Assert.Equal(shapes.Count, shapes.Select(s => s.Id).Distinct().Count());
        Assert.Equal(shapes.Count, shapes.Select(s => s.BaseMask).Distinct().Count());
    }

    [Theory]
    [InlineData(PieceCategory.Tiny, 1)]
    [InlineData(PieceCategory.Small, 8)]
    [InlineData(PieceCategory.Medium, 19)]
    [InlineData(PieceCategory.Large, 9)]
    public void Each_category_has_the_expected_number_of_shapes(PieceCategory category, int expected)
    {
        Assert.Equal(expected, PieceCatalog.Standard.Shapes.Count(s => s.Category == category));
    }

    [Fact]
    public void Categories_match_cell_counts()
    {
        foreach (var shape in PieceCatalog.Standard.Shapes)
        {
            var expected = shape.CellCount switch
            {
                1 => PieceCategory.Tiny,
                2 or 3 => PieceCategory.Small,
                4 => PieceCategory.Medium,
                _ => PieceCategory.Large,
            };
            Assert.Equal(expected, shape.Category);
        }
    }

    [Fact]
    public void Shapes_can_be_looked_up_by_id()
    {
        Assert.Equal(9, PieceCatalog.Standard["o3"].CellCount);
        Assert.True(PieceCatalog.Standard.TryGet("i5h", out var line));
        Assert.Equal(5, line!.Width);
        Assert.False(PieceCatalog.Standard.TryGet("nope", out _));
        Assert.Throws<KeyNotFoundException>(() => PieceCatalog.Standard["nope"]);
    }

    [Fact]
    public void Duplicate_ids_are_rejected()
    {
        var shapes = new[]
        {
            PieceShape.FromRows("a", PieceCategory.Tiny, "#"),
            PieceShape.FromRows("a", PieceCategory.Small, "##"),
        };

        Assert.Throws<ArgumentException>(() => new PieceCatalog(shapes));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'PieceShape' could not be found`.

- [ ] **Step 3: Implement the shapes and the catalog**

`core/src/PieceCategory.cs`:
```csharp
namespace BlockPuzzle.Core
{
    /// <summary>Size groups that the generator weights against each other.</summary>
    public enum PieceCategory
    {
        /// <summary>A single cell. Only dealt by mercy weights or as a last resort.</summary>
        Tiny = 0,
        /// <summary>2-3 cells.</summary>
        Small = 1,
        /// <summary>4 cells.</summary>
        Medium = 2,
        /// <summary>5-9 cells.</summary>
        Large = 3,
    }
}
```

`core/src/PieceShape.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// One piece shape. Pieces never rotate, so each orientation is its own shape.
    /// Every legal position on the board is precomputed as a cell mask.
    /// </summary>
    public sealed class PieceShape
    {
        readonly ulong[] _placements;
        readonly int[] _anchors; // bit index of the top-left corner of each placement

        public string Id { get; }
        public PieceCategory Category { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; }
        /// <summary>The shape with its top-left corner at row 0, column 0.</summary>
        public ulong BaseMask { get; }

        PieceShape(string id, PieceCategory category, int width, int height, int cellCount, ulong baseMask)
        {
            Id = id;
            Category = category;
            Width = width;
            Height = height;
            CellCount = cellCount;
            BaseMask = baseMask;

            int count = (Board.Size - height + 1) * (Board.Size - width + 1);
            _placements = new ulong[count];
            _anchors = new int[count];
            int i = 0;
            for (int row = 0; row + height <= Board.Size; row++)
            {
                for (int col = 0; col + width <= Board.Size; col++)
                {
                    int anchor = Board.BitIndex(row, col);
                    _placements[i] = baseMask << anchor;
                    _anchors[i] = anchor;
                    i++;
                }
            }
        }

        /// <summary>Every position where the shape stays inside the board, as cell masks.</summary>
        public ReadOnlySpan<ulong> Placements => _placements;

        public int PlacementCount => _placements.Length;

        public int AnchorRow(int placementIndex) => _anchors[placementIndex] / Board.Size;

        public int AnchorCol(int placementIndex) => _anchors[placementIndex] % Board.Size;

        /// <summary>The mask with the top-left corner at (row, col); false if the piece would leave the board.</summary>
        public bool TryGetMask(int row, int col, out ulong mask)
        {
            if (row < 0 || col < 0 || row + Height > Board.Size || col + Width > Board.Size)
            {
                mask = 0;
                return false;
            }
            mask = BaseMask << Board.BitIndex(row, col);
            return true;
        }

        public bool FitsAnywhere(Board board)
        {
            foreach (ulong placement in _placements)
            {
                if (board.Fits(placement)) return true;
            }
            return false;
        }

        /// <summary>True if some placement on this board would complete at least one line.</summary>
        public bool CanClearLine(Board board)
        {
            foreach (ulong placement in _placements)
            {
                if (board.Fits(placement) && board.Place(placement).Lines > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a shape from text art: '#' = cell, '.' = gap. The art must be tight:
        /// no empty first or last row or column.
        /// </summary>
        public static PieceShape FromRows(string id, PieceCategory category, params string[] rows)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("An id is required.", nameof(id));
            if (rows == null || rows.Length == 0) throw new ArgumentException("At least one row is required.", nameof(rows));

            int height = rows.Length;
            int width = rows[0].Length;
            if (height > Board.Size || width == 0 || width > Board.Size)
                throw new ArgumentException("The shape must fit on the board.", nameof(rows));

            ulong mask = 0;
            int cells = 0;
            for (int r = 0; r < height; r++)
            {
                if (rows[r].Length != width) throw new ArgumentException($"Row {r} has a different length.", nameof(rows));
                for (int c = 0; c < width; c++)
                {
                    char ch = rows[r][c];
                    if (ch == '#')
                    {
                        mask |= 1UL << Board.BitIndex(r, c);
                        cells++;
                    }
                    else if (ch != '.')
                    {
                        throw new ArgumentException($"Unexpected character '{ch}'.", nameof(rows));
                    }
                }
            }

            if (!RowHasCell(mask, 0) || !RowHasCell(mask, height - 1) || !ColumnHasCell(mask, 0) || !ColumnHasCell(mask, width - 1))
                throw new ArgumentException("The art must not have an empty edge row or column.", nameof(rows));

            return new PieceShape(id, category, width, height, cells, mask);
        }

        static bool RowHasCell(ulong mask, int row) => (mask & (0xFFUL << (row * Board.Size))) != 0;

        static bool ColumnHasCell(ulong mask, int col) => (mask & (0x0101010101010101UL << col)) != 0;

        public override string ToString() => Id;
    }
}
```

`core/src/PieceCatalog.cs`:
```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BlockPuzzle.Core
{
    /// <summary>The set of shapes the generator can deal.</summary>
    public sealed class PieceCatalog
    {
        readonly PieceShape[] _shapes;
        readonly Dictionary<string, PieceShape> _byId = new Dictionary<string, PieceShape>(StringComparer.Ordinal);

        public PieceCatalog(IEnumerable<PieceShape> shapes)
        {
            if (shapes == null) throw new ArgumentNullException(nameof(shapes));
            _shapes = new List<PieceShape>(shapes).ToArray();
            if (_shapes.Length == 0) throw new ArgumentException("A catalog needs at least one shape.", nameof(shapes));
            foreach (var shape in _shapes)
            {
                if (_byId.ContainsKey(shape.Id)) throw new ArgumentException($"Duplicate shape id '{shape.Id}'.", nameof(shapes));
                _byId.Add(shape.Id, shape);
            }
        }

        /// <summary>The standard set: 1 tiny, 8 small, 19 medium and 9 large shapes.</summary>
        public static PieceCatalog Standard { get; } = CreateStandard();

        public IReadOnlyList<PieceShape> Shapes => _shapes;

        public PieceShape this[string id] =>
            _byId.TryGetValue(id, out var shape) ? shape : throw new KeyNotFoundException($"Unknown shape id '{id}'.");

        public bool TryGet(string id, [NotNullWhen(true)] out PieceShape? shape) => _byId.TryGetValue(id, out shape);

        static PieceCatalog CreateStandard()
        {
            const PieceCategory tiny = PieceCategory.Tiny;
            const PieceCategory small = PieceCategory.Small;
            const PieceCategory medium = PieceCategory.Medium;
            const PieceCategory large = PieceCategory.Large;

            return new PieceCatalog(new[]
            {
                PieceShape.FromRows("dot", tiny, "#"),

                PieceShape.FromRows("i2h", small, "##"),
                PieceShape.FromRows("i2v", small, "#", "#"),
                PieceShape.FromRows("i3h", small, "###"),
                PieceShape.FromRows("i3v", small, "#", "#", "#"),
                PieceShape.FromRows("c3a", small, "##", "#."),
                PieceShape.FromRows("c3b", small, "##", ".#"),
                PieceShape.FromRows("c3c", small, "#.", "##"),
                PieceShape.FromRows("c3d", small, ".#", "##"),

                PieceShape.FromRows("i4h", medium, "####"),
                PieceShape.FromRows("i4v", medium, "#", "#", "#", "#"),
                PieceShape.FromRows("o2", medium, "##", "##"),
                PieceShape.FromRows("l4a", medium, "#.", "#.", "##"),
                PieceShape.FromRows("l4b", medium, "###", "#.."),
                PieceShape.FromRows("l4c", medium, "##", ".#", ".#"),
                PieceShape.FromRows("l4d", medium, "..#", "###"),
                PieceShape.FromRows("j4a", medium, ".#", ".#", "##"),
                PieceShape.FromRows("j4b", medium, "#..", "###"),
                PieceShape.FromRows("j4c", medium, "##", "#.", "#."),
                PieceShape.FromRows("j4d", medium, "###", "..#"),
                PieceShape.FromRows("t4a", medium, "###", ".#."),
                PieceShape.FromRows("t4b", medium, ".#", "##", ".#"),
                PieceShape.FromRows("t4c", medium, ".#.", "###"),
                PieceShape.FromRows("t4d", medium, "#.", "##", "#."),
                PieceShape.FromRows("s4h", medium, ".##", "##."),
                PieceShape.FromRows("s4v", medium, "#.", "##", ".#"),
                PieceShape.FromRows("z4h", medium, "##.", ".##"),
                PieceShape.FromRows("z4v", medium, ".#", "##", "#."),

                PieceShape.FromRows("i5h", large, "#####"),
                PieceShape.FromRows("i5v", large, "#", "#", "#", "#", "#"),
                PieceShape.FromRows("r23", large, "###", "###"),
                PieceShape.FromRows("r32", large, "##", "##", "##"),
                PieceShape.FromRows("o3", large, "###", "###", "###"),
                PieceShape.FromRows("c5a", large, "###", "#..", "#.."),
                PieceShape.FromRows("c5b", large, "###", "..#", "..#"),
                PieceShape.FromRows("c5c", large, "#..", "#..", "###"),
                PieceShape.FromRows("c5d", large, "..#", "..#", "###"),
            });
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 26`.

- [ ] **Step 5: Commit**

```bash
git add core
git commit -m "Add piece shapes and the standard 37-shape catalog" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Seeded random generator (PCG32)

**Files:**
- Create: `core/src/Pcg32.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/Pcg32Tests.cs`

**Interfaces:**
- Produces: `sealed class Pcg32`, with:
  - `Pcg32(ulong seed, ulong sequence = 54)`, `const ulong DefaultSequence = 54`
  - `uint NextUInt()`, `int NextInt(int bound)`
  - `ulong State`, `ulong Increment`, `static Pcg32 FromState(ulong state, ulong increment)`

- [ ] **Step 1: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/Pcg32Tests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class Pcg32Tests
{
    [Fact]
    public void Matches_the_reference_implementation()
    {
        // pcg32_srandom_r(&rng, 42u, 54u) followed by pcg32_random_r, from the PCG reference demo.
        var rng = new Pcg32(42, 54);
        uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };

        foreach (uint value in expected) Assert.Equal(value, rng.NextUInt());
    }

    [Fact]
    public void The_same_seed_gives_the_same_sequence()
    {
        var a = new Pcg32(2026);
        var b = new Pcg32(2026);

        for (int i = 0; i < 1000; i++) Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void NextInt_stays_in_range_and_reaches_every_value()
    {
        var rng = new Pcg32(5);
        var seen = new bool[7];

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.NextInt(7);
            Assert.InRange(value, 0, 6);
            seen[value] = true;
        }

        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextInt_rejects_bounds_below_one()
    {
        var rng = new Pcg32(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-1));
    }

    [Fact]
    public void A_restored_generator_continues_the_same_sequence()
    {
        var original = new Pcg32(123);
        for (int i = 0; i < 10; i++) original.NextUInt();

        var restored = Pcg32.FromState(original.State, original.Increment);

        for (int i = 0; i < 100; i++) Assert.Equal(original.NextUInt(), restored.NextUInt());
    }

    [Fact]
    public void FromState_rejects_an_even_increment()
    {
        Assert.Throws<ArgumentException>(() => Pcg32.FromState(1, 2));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'Pcg32' could not be found`.

- [ ] **Step 3: Implement `Pcg32`**

`core/src/Pcg32.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// PCG32 (XSH-RR) random generator by Melissa O'Neill. Pure integer math, so it produces the
    /// same sequence on every platform. Use this, never System.Random, in game logic.
    /// </summary>
    public sealed class Pcg32
    {
        public const ulong DefaultSequence = 54;
        const ulong Multiplier = 6364136223846793005UL;

        ulong _state;
        readonly ulong _increment;

        public Pcg32(ulong seed, ulong sequence = DefaultSequence)
        {
            _state = 0;
            _increment = (sequence << 1) | 1UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        Pcg32(ulong state, ulong increment, bool restoring)
        {
            _state = state;
            _increment = increment;
        }

        /// <summary>The internal state; save it with Increment to resume the sequence later.</summary>
        public ulong State => _state;

        public ulong Increment => _increment;

        public static Pcg32 FromState(ulong state, ulong increment)
        {
            if ((increment & 1UL) == 0) throw new ArgumentException("The increment must be odd.", nameof(increment));
            return new Pcg32(state, increment, restoring: true);
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = unchecked((uint)(((old >> 18) ^ old) >> 27));
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>A uniform integer in [0, bound), without modulo bias.</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound), "The bound must be positive.");
            uint unsignedBound = (uint)bound;
            uint threshold = unchecked((uint)(-(long)unsignedBound)) % unsignedBound;
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold) return (int)(value % unsignedBound);
            }
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 32`.

- [ ] **Step 5: Commit**

```bash
git add core
git commit -m "Add the PCG32 seeded random generator" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Scoring

**Files:**
- Create: `core/src/ScoringSettings.cs`, `core/src/Scoring.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/ScoringTests.cs`

**Interfaces:**
- Produces:
  - `[Serializable] sealed class ScoringSettings`: public fields `PointsPerCell = 1`, `LineBase = 10`, `StreakBonusPercent = 50`, `StreakResetAfterMisses = 3`, `BoardClearBonus = 300`
  - `readonly struct ScoreState`: `Score`, `Streak`, `MissesSinceClear`, `MaxStreak`, and constructor `ScoreState(int score, int streak, int missesSinceClear, int maxStreak)`
  - `static class Scoring`, with:
    - `int LinePoints(ScoringSettings, int lines)`
    - `ScoreState Apply(ScoringSettings, ScoreState, int cellsPlaced, int lines, bool boardCleared, out int points)`

- [ ] **Step 1: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/ScoringTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class ScoringTests
{
    static readonly ScoringSettings Defaults = new();

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 30)]
    [InlineData(3, 60)]
    [InlineData(4, 100)]
    public void Line_points_grow_with_lines_cleared_at_once(int lines, int expected)
    {
        Assert.Equal(expected, Scoring.LinePoints(Defaults, lines));
    }

    [Fact]
    public void A_placement_without_clears_scores_its_cells()
    {
        var state = Scoring.Apply(Defaults, default, cellsPlaced: 4, lines: 0, boardCleared: false, out int points);

        Assert.Equal(4, points);
        Assert.Equal(4, state.Score);
        Assert.Equal(0, state.Streak);
    }

    [Fact]
    public void Consecutive_clears_build_a_streak_multiplier()
    {
        var first = Scoring.Apply(Defaults, default, 3, 1, false, out int p1);   // streak 1: 3 + 10
        var second = Scoring.Apply(Defaults, first, 3, 1, false, out int p2);    // streak 2: 3 + 10 * 1.5
        var third = Scoring.Apply(Defaults, second, 3, 2, false, out int p3);    // streak 3: 3 + 30 * 2.0

        Assert.Equal(13, p1);
        Assert.Equal(18, p2);
        Assert.Equal(63, p3);
        Assert.Equal(3, third.Streak);
        Assert.Equal(3, third.MaxStreak);
        Assert.Equal(13 + 18 + 63, third.Score);
    }

    [Fact]
    public void The_streak_survives_two_misses_and_resets_on_the_third()
    {
        var state = Scoring.Apply(Defaults, default, 1, 1, false, out _);
        state = Scoring.Apply(Defaults, state, 1, 0, false, out _);
        state = Scoring.Apply(Defaults, state, 1, 0, false, out _);

        Assert.Equal(1, state.Streak);
        Assert.Equal(2, Scoring.Apply(Defaults, state, 1, 1, false, out _).Streak);

        state = Scoring.Apply(Defaults, state, 1, 0, false, out _);

        Assert.Equal(0, state.Streak);
        Assert.Equal(1, Scoring.Apply(Defaults, state, 1, 1, false, out _).Streak);
    }

    [Fact]
    public void Clearing_the_whole_board_adds_the_bonus()
    {
        Scoring.Apply(Defaults, default, 1, 1, boardCleared: true, out int points);

        Assert.Equal(1 + 10 + 300, points);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'ScoringSettings' could not be found`.

- [ ] **Step 3: Implement scoring**

`core/src/ScoringSettings.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Scoring values. Public fields so Unity's JsonUtility and Remote Config can fill them.</summary>
    [Serializable]
    public sealed class ScoringSettings
    {
        public int PointsPerCell = 1;
        /// <summary>Clearing n lines at once scores LineBase * n(n+1)/2: 10, 30, 60, 100...</summary>
        public int LineBase = 10;
        /// <summary>Each streak level after the first adds this percentage to line points.</summary>
        public int StreakBonusPercent = 50;
        /// <summary>The streak resets after this many placements in a row without a clear.</summary>
        public int StreakResetAfterMisses = 3;
        public int BoardClearBonus = 300;
    }
}
```

`core/src/Scoring.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Running score values for one game.</summary>
    public readonly struct ScoreState
    {
        public readonly int Score;
        public readonly int Streak;
        public readonly int MissesSinceClear;
        public readonly int MaxStreak;

        public ScoreState(int score, int streak, int missesSinceClear, int maxStreak)
        {
            Score = score;
            Streak = streak;
            MissesSinceClear = missesSinceClear;
            MaxStreak = maxStreak;
        }
    }

    public static class Scoring
    {
        public static int LinePoints(ScoringSettings settings, int lines) =>
            lines <= 0 ? 0 : settings.LineBase * lines * (lines + 1) / 2;

        /// <summary>Scores one placement and returns the new running state.</summary>
        public static ScoreState Apply(ScoringSettings settings, ScoreState state, int cellsPlaced, int lines, bool boardCleared, out int points)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            int streak = state.Streak;
            int misses = state.MissesSinceClear;
            int linePoints = 0;
            if (lines > 0)
            {
                streak++;
                misses = 0;
                int multiplierPercent = 100 + settings.StreakBonusPercent * (streak - 1);
                linePoints = LinePoints(settings, lines) * multiplierPercent / 100;
            }
            else
            {
                misses++;
                if (misses >= settings.StreakResetAfterMisses) streak = 0;
            }

            points = cellsPlaced * settings.PointsPerCell + linePoints + (boardCleared ? settings.BoardClearBonus : 0);
            return new ScoreState(state.Score + points, streak, misses, Math.Max(state.MaxStreak, streak));
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 40`.

- [ ] **Step 5: Commit**

```bash
git add core
git commit -m "Add scoring with multi-line bonus, streaks and board clears" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Generator settings, tray solver and piece generator

**Files:**
- Create: `core/src/FitGuarantee.cs`, `core/src/DifficultyBand.cs`, `core/src/GeneratorSettings.cs`, `core/src/TraySolver.cs`, `core/src/PieceGenerator.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/GeneratorSettingsTests.cs`, `core/dotnet/BlockPuzzle.Core.Tests/TraySolverTests.cs`, `core/dotnet/BlockPuzzle.Core.Tests/PieceGeneratorTests.cs`

**Interfaces:**
- Consumes: `Board`, `PieceShape` (`FitsAnywhere`, `CanClearLine`, `Placements`, `CellCount`), `PieceCatalog`, `PieceCategory`, `Pcg32`
- Produces:
  - `enum FitGuarantee { None, AtLeastOne, WholeTray }`
  - `[Serializable] sealed class DifficultyBand`:
    - public fields `FromPlacement`, `Tiny`, `Small`, `Medium`, `Large`
    - constructor `DifficultyBand(int fromPlacement, int tiny, int small, int medium, int large)`
    - `int WeightOf(PieceCategory)`
  - `[Serializable] sealed class GeneratorSettings`:
    - public fields `Name`, `FitGuarantee`, `Bands`, `HelpfulUntilPlacement`, `HelpfulChancePercent`, `MercyAfterQuickLosses`, `QuickLossPlacements`, `MercyPlacements`, `Mercy`, `MaxWholeTrayAttempts`
    - `DifficultyBand BandFor(int placements)`, `void Validate()`
    - presets `Gentle()`, `Baseline()`, `Hard()`, `ForProfile(string)`
  - `static class TraySolver`: `bool CanPlaceAll(Board board, PieceShape?[] pieces)`
  - `sealed class PieceGenerator`:
    - `const int TraySize = 3`
    - constructor `PieceGenerator(PieceCatalog, GeneratorSettings, Pcg32)`
    - `Pcg32 Rng`
    - `PieceShape[] Deal(Board board, int placements, bool mercyGame)`, `PieceShape[] DealRescue(Board board)`

- [ ] **Step 1: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/GeneratorSettingsTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class GeneratorSettingsTests
{
    [Theory]
    [InlineData("gentle")]
    [InlineData("baseline")]
    [InlineData("hard")]
    public void Presets_are_valid(string name)
    {
        var settings = GeneratorSettings.ForProfile(name);

        settings.Validate();
        Assert.Equal(name, settings.Name);
    }

    [Fact]
    public void An_unknown_profile_throws()
    {
        Assert.Throws<ArgumentException>(() => GeneratorSettings.ForProfile("nope"));
    }

    [Fact]
    public void BandFor_picks_the_last_band_that_has_started()
    {
        var settings = new GeneratorSettings
        {
            Bands = new[]
            {
                new DifficultyBand(0, 0, 1, 0, 0),
                new DifficultyBand(30, 0, 0, 1, 0),
                new DifficultyBand(90, 0, 0, 0, 1),
            },
        };

        Assert.Same(settings.Bands[0], settings.BandFor(0));
        Assert.Same(settings.Bands[0], settings.BandFor(29));
        Assert.Same(settings.Bands[1], settings.BandFor(30));
        Assert.Same(settings.Bands[2], settings.BandFor(500));
    }

    [Fact]
    public void Validate_rejects_settings_that_cannot_work()
    {
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = Array.Empty<DifficultyBand>() }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(5, 0, 1, 1, 1) } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings
        {
            Bands = new[] { new DifficultyBand(0, 0, 1, 1, 1), new DifficultyBand(0, 0, 1, 1, 1) },
        }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(0, 0, -1, 1, 1) } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { Bands = new[] { new DifficultyBand(0, 0, 0, 0, 0) } }.Validate());
        Assert.Throws<ArgumentException>(() => new GeneratorSettings { HelpfulChancePercent = 101 }.Validate());
    }
}
```

`core/dotnet/BlockPuzzle.Core.Tests/TraySolverTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class TraySolverTests
{
    static PieceShape Shape(string id) => PieceCatalog.Standard[id];

    [Fact]
    public void Three_big_squares_fit_on_an_empty_board()
    {
        Assert.True(TraySolver.CanPlaceAll(Board.Empty, new[] { Shape("o3"), Shape("o3"), Shape("o3") }));
    }

    [Fact]
    public void A_piece_that_fits_nowhere_fails()
    {
        Assert.False(TraySolver.CanPlaceAll(TestBoards.Checkerboard, new[] { Shape("dot"), Shape("i2h") }));
    }

    [Fact]
    public void Order_matters_because_clears_free_space()
    {
        // Only small pieces fit at first. The dot completes row 0, which then has room for the 5-long line.
        var board = Board.Parse(
            "#######.",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.");

        Assert.False(Shape("i5h").FitsAnywhere(board));
        Assert.True(TraySolver.CanPlaceAll(board, new[] { Shape("i5h"), Shape("dot") }));
        Assert.False(TraySolver.CanPlaceAll(board, new[] { Shape("i5h"), Shape("i3h") }));
    }

    [Fact]
    public void Used_slots_are_skipped()
    {
        Assert.True(TraySolver.CanPlaceAll(TestBoards.Checkerboard, new PieceShape?[] { null, Shape("dot"), null }));
    }
}
```

`core/dotnet/BlockPuzzle.Core.Tests/PieceGeneratorTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class PieceGeneratorTests
{
    static PieceGenerator Create(GeneratorSettings settings, ulong seed = 1) =>
        new(PieceCatalog.Standard, settings, new Pcg32(seed));

    static string Ids(PieceShape[] tray) => string.Join(",", tray.Select(p => p.Id));

    static readonly Board AlmostFullRow = Board.Parse(
        "######..",
        "........",
        "........",
        "........",
        "........",
        "........",
        "........",
        "........");

    [Fact]
    public void Trays_have_three_pieces()
    {
        var tray = Create(GeneratorSettings.Baseline()).Deal(Board.Empty, placements: 0, mercyGame: false);

        Assert.Equal(PieceGenerator.TraySize, tray.Length);
        Assert.All(tray, p => Assert.NotNull(p));
    }

    [Fact]
    public void The_same_seed_deals_the_same_trays()
    {
        var a = Create(GeneratorSettings.Baseline(), seed: 7);
        var b = Create(GeneratorSettings.Baseline(), seed: 7);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(Ids(a.Deal(AlmostFullRow, i * 3, mercyGame: false)), Ids(b.Deal(AlmostFullRow, i * 3, mercyGame: false)));
        }
    }

    [Fact]
    public void Different_seeds_deal_different_trays()
    {
        var a = Create(GeneratorSettings.Baseline(), seed: 1);
        var b = Create(GeneratorSettings.Baseline(), seed: 2);

        var first = Enumerable.Range(0, 20).Select(_ => Ids(a.Deal(Board.Empty, 0, false))).ToArray();
        var second = Enumerable.Range(0, 20).Select(_ => Ids(b.Deal(Board.Empty, 0, false))).ToArray();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void At_least_one_piece_fits_even_on_a_crowded_board()
    {
        var generator = Create(GeneratorSettings.Baseline());

        for (int i = 0; i < 200; i++)
        {
            var tray = generator.Deal(TestBoards.Checkerboard, placements: 50, mercyGame: false);
            Assert.Contains(tray, p => p.FitsAnywhere(TestBoards.Checkerboard));
        }
    }

    [Fact]
    public void Whole_tray_deals_can_be_placed()
    {
        var generator = Create(GeneratorSettings.Gentle());
        var board = Board.Parse(
            "####....",
            "####....",
            "####....",
            "####....",
            "........",
            "........",
            "........",
            "........");

        for (int i = 0; i < 200; i++)
        {
            Assert.True(TraySolver.CanPlaceAll(board, generator.Deal(board, placements: 10, mercyGame: false)));
        }
    }

    [Fact]
    public void Mercy_deals_smaller_pieces()
    {
        double AverageCells(bool mercyGame)
        {
            var generator = Create(GeneratorSettings.Baseline(), seed: 3);
            return Enumerable.Range(0, 2000)
                .SelectMany(_ => generator.Deal(Board.Empty, 0, mercyGame))
                .Average(p => p.CellCount);
        }

        Assert.True(AverageCells(mercyGame: true) < AverageCells(mercyGame: false) - 0.3);
    }

    [Fact]
    public void The_helpful_bias_prefers_pieces_that_complete_a_line()
    {
        double ClearingShare(int chancePercent)
        {
            var settings = GeneratorSettings.Baseline();
            settings.HelpfulChancePercent = chancePercent;
            var generator = Create(settings, seed: 5);
            var pieces = Enumerable.Range(0, 1000).SelectMany(_ => generator.Deal(AlmostFullRow, 0, false)).ToArray();
            return pieces.Count(p => p.CanClearLine(AlmostFullRow)) / (double)pieces.Length;
        }

        Assert.True(ClearingShare(100) > ClearingShare(0) + 0.3);
    }

    [Fact]
    public void The_rescue_tray_always_fits()
    {
        var generator = Create(GeneratorSettings.Baseline());

        Assert.True(TraySolver.CanPlaceAll(TestBoards.Checkerboard, generator.DealRescue(TestBoards.Checkerboard)));
        Assert.True(TraySolver.CanPlaceAll(AlmostFullRow, generator.DealRescue(AlmostFullRow)));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'GeneratorSettings' could not be found`.

- [ ] **Step 3: Implement the settings**

`core/src/FitGuarantee.cs`:
```csharp
namespace BlockPuzzle.Core
{
    /// <summary>What the generator promises about a freshly dealt tray.</summary>
    public enum FitGuarantee
    {
        /// <summary>No promise: a fresh tray may end the game.</summary>
        None = 0,
        /// <summary>At least one of the three pieces fits somewhere.</summary>
        AtLeastOne = 1,
        /// <summary>All three can be placed in some order, counting line clears in between.</summary>
        WholeTray = 2,
    }
}
```

`core/src/DifficultyBand.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// Category weights that apply from a given placement onward. Weights are relative integers;
    /// they don't need to add up to 100.
    /// </summary>
    [Serializable]
    public sealed class DifficultyBand
    {
        public int FromPlacement;
        public int Tiny;
        public int Small;
        public int Medium;
        public int Large;

        public DifficultyBand() { }

        public DifficultyBand(int fromPlacement, int tiny, int small, int medium, int large)
        {
            FromPlacement = fromPlacement;
            Tiny = tiny;
            Small = small;
            Medium = medium;
            Large = large;
        }

        public int WeightOf(PieceCategory category)
        {
            switch (category)
            {
                case PieceCategory.Tiny: return Tiny;
                case PieceCategory.Small: return Small;
                case PieceCategory.Medium: return Medium;
                case PieceCategory.Large: return Large;
                default: return 0;
            }
        }
    }
}
```

`core/src/GeneratorSettings.cs` (preset weights are starting values; Task 8 tunes them with the simulator):
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// Tuning values for the piece generator. Public fields so Unity's JsonUtility and the
    /// Remote Config loader can fill them.
    /// </summary>
    [Serializable]
    public sealed class GeneratorSettings
    {
        public string Name = "custom";
        public FitGuarantee FitGuarantee = FitGuarantee.AtLeastOne;
        /// <summary>Category weights by game progress, sorted by FromPlacement; the first band starts at 0.</summary>
        public DifficultyBand[] Bands = { new DifficultyBand(0, 0, 35, 45, 20) };
        /// <summary>Until this placement, a slot may prefer a piece that can complete a line.</summary>
        public int HelpfulUntilPlacement = 15;
        /// <summary>Chance (0-100) that a slot uses that preference while it applies.</summary>
        public int HelpfulChancePercent = 50;
        /// <summary>After this many quick losses in a row, the next game starts with mercy weights. 0 turns mercy off.</summary>
        public int MercyAfterQuickLosses = 2;
        /// <summary>A game that ends before this many placements counts as a quick loss.</summary>
        public int QuickLossPlacements = 20;
        /// <summary>How many placements of a mercy game use the mercy weights.</summary>
        public int MercyPlacements = 30;
        public DifficultyBand Mercy = new DifficultyBand(0, 5, 55, 35, 5);
        /// <summary>Re-deal attempts for WholeTray before falling back to AtLeastOne.</summary>
        public int MaxWholeTrayAttempts = 20;

        public DifficultyBand BandFor(int placements)
        {
            DifficultyBand band = Bands[0];
            for (int i = 1; i < Bands.Length && Bands[i].FromPlacement <= placements; i++) band = Bands[i];
            return band;
        }

        /// <summary>Throws if these settings can't work (for example, a mistyped Remote Config value).</summary>
        public void Validate()
        {
            if (Bands == null || Bands.Length == 0) throw new ArgumentException("At least one difficulty band is required.");
            if (Bands[0].FromPlacement != 0) throw new ArgumentException("The first difficulty band must start at placement 0.");
            for (int i = 0; i < Bands.Length; i++)
            {
                ValidateBand(Bands[i], $"Bands[{i}]");
                if (i > 0 && Bands[i].FromPlacement <= Bands[i - 1].FromPlacement)
                    throw new ArgumentException("Difficulty bands must be sorted by FromPlacement.");
            }
            ValidateBand(Mercy, nameof(Mercy));
            if (HelpfulChancePercent < 0 || HelpfulChancePercent > 100)
                throw new ArgumentException("HelpfulChancePercent must be between 0 and 100.");
            if (MaxWholeTrayAttempts < 1) throw new ArgumentException("MaxWholeTrayAttempts must be at least 1.");
        }

        static void ValidateBand(DifficultyBand band, string name)
        {
            if (band == null) throw new ArgumentException($"{name} is missing.");
            if (band.Tiny < 0 || band.Small < 0 || band.Medium < 0 || band.Large < 0)
                throw new ArgumentException($"{name} has a negative weight.");
            if (band.Tiny + band.Small + band.Medium + band.Large == 0)
                throw new ArgumentException($"{name} has no positive weight.");
        }

        /// <summary>Easier: every fresh tray can be placed, more small pieces, longer helpful phase.</summary>
        public static GeneratorSettings Gentle() => new GeneratorSettings
        {
            Name = "gentle",
            FitGuarantee = FitGuarantee.WholeTray,
            Bands = new[]
            {
                new DifficultyBand(0, 0, 45, 42, 13),
                new DifficultyBand(40, 0, 38, 44, 18),
                new DifficultyBand(120, 0, 30, 45, 25),
            },
            HelpfulUntilPlacement = 30,
            HelpfulChancePercent = 70,
        };

        /// <summary>The default for new players.</summary>
        public static GeneratorSettings Baseline() => new GeneratorSettings
        {
            Name = "baseline",
            Bands = new[]
            {
                new DifficultyBand(0, 0, 35, 45, 20),
                new DifficultyBand(30, 0, 28, 45, 27),
                new DifficultyBand(90, 0, 22, 43, 35),
            },
        };

        /// <summary>Harder: more large pieces sooner, no helpful phase.</summary>
        public static GeneratorSettings Hard() => new GeneratorSettings
        {
            Name = "hard",
            Bands = new[]
            {
                new DifficultyBand(0, 0, 25, 45, 30),
                new DifficultyBand(25, 0, 20, 42, 38),
                new DifficultyBand(70, 0, 15, 40, 45),
            },
            HelpfulUntilPlacement = 0,
        };

        public static GeneratorSettings ForProfile(string name)
        {
            switch (name)
            {
                case "gentle": return Gentle();
                case "baseline": return Baseline();
                case "hard": return Hard();
                default: throw new ArgumentException($"Unknown generator profile '{name}'.", nameof(name));
            }
        }
    }
}
```

- [ ] **Step 4: Implement the tray solver and the generator**

`core/src/TraySolver.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Checks whether pieces can all be placed in some order, counting line clears in between.</summary>
    public static class TraySolver
    {
        /// <param name="pieces">At most three pieces; null entries (used tray slots) are skipped.</param>
        public static bool CanPlaceAll(Board board, PieceShape?[] pieces)
        {
            if (pieces == null) throw new ArgumentNullException(nameof(pieces));
            if (pieces.Length > PieceGenerator.TraySize) throw new ArgumentException("At most three pieces are supported.", nameof(pieces));

            int used = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null) used |= 1 << i;
            }
            return Solve(board, pieces, used);
        }

        static bool Solve(Board board, PieceShape?[] pieces, int used)
        {
            bool anyLeft = false;
            for (int i = 0; i < pieces.Length; i++)
            {
                if ((used & (1 << i)) != 0) continue;
                anyLeft = true;
                if (SameAsEarlierUnused(pieces, used, i)) continue; // same subtree as the earlier copy

                foreach (ulong placement in pieces[i]!.Placements)
                {
                    if (board.Fits(placement) && Solve(board.Place(placement).Board, pieces, used | (1 << i))) return true;
                }
            }
            return !anyLeft;
        }

        static bool SameAsEarlierUnused(PieceShape?[] pieces, int used, int index)
        {
            for (int j = 0; j < index; j++)
            {
                if ((used & (1 << j)) == 0 && ReferenceEquals(pieces[j], pieces[index])) return true;
            }
            return false;
        }
    }
}
```

`core/src/PieceGenerator.cs`:
```csharp
#nullable enable
using System;
using System.Collections.Generic;

namespace BlockPuzzle.Core
{
    /// <summary>Deals trays of three pieces. Deterministic for a given random generator state.</summary>
    public sealed class PieceGenerator
    {
        public const int TraySize = 3;
        const int CategoryCount = 4;

        enum Filter { None, FitsAnywhere, CanClearLine }

        readonly GeneratorSettings _settings;
        readonly Pcg32 _rng;
        readonly PieceShape[][] _byCategory;
        readonly List<PieceShape>[] _allowed; // scratch lists, reused on every pick
        readonly PieceShape _smallest;

        public PieceGenerator(PieceCatalog catalog, GeneratorSettings settings, Pcg32 rng)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            settings.Validate();

            var lists = new List<PieceShape>[CategoryCount];
            _allowed = new List<PieceShape>[CategoryCount];
            for (int c = 0; c < CategoryCount; c++)
            {
                lists[c] = new List<PieceShape>();
                _allowed[c] = new List<PieceShape>();
            }

            _smallest = catalog.Shapes[0];
            foreach (var shape in catalog.Shapes)
            {
                lists[(int)shape.Category].Add(shape);
                if (shape.CellCount < _smallest.CellCount) _smallest = shape;
            }

            _byCategory = new PieceShape[CategoryCount][];
            for (int c = 0; c < CategoryCount; c++) _byCategory[c] = lists[c].ToArray();
        }

        /// <summary>The random generator, so a session can save and restore its exact position.</summary>
        public Pcg32 Rng => _rng;

        /// <summary>Deals a tray for the board.</summary>
        /// <param name="placements">Placements made so far in this game; picks the difficulty band.</param>
        /// <param name="mercyGame">True if this game started after too many quick losses.</param>
        public PieceShape[] Deal(Board board, int placements, bool mercyGame)
        {
            DifficultyBand band = mercyGame && placements < _settings.MercyPlacements ? _settings.Mercy : _settings.BandFor(placements);
            bool helpful = placements < _settings.HelpfulUntilPlacement;

            if (_settings.FitGuarantee == FitGuarantee.WholeTray)
            {
                for (int attempt = 0; attempt < _settings.MaxWholeTrayAttempts; attempt++)
                {
                    var tray = DealRaw(board, band, helpful);
                    if (TraySolver.CanPlaceAll(board, tray)) return tray;
                }
            }

            var dealt = DealRaw(board, band, helpful);
            return _settings.FitGuarantee == FitGuarantee.None ? dealt : EnsureOneFits(board, dealt, band);
        }

        /// <summary>The rewarded revive: small pieces that can all be placed on this board.</summary>
        public PieceShape[] DealRescue(Board board)
        {
            for (int attempt = 0; attempt < _settings.MaxWholeTrayAttempts; attempt++)
            {
                var tray = DealRaw(board, _settings.Mercy, helpful: true);
                if (TraySolver.CanPlaceAll(board, tray)) return tray;
            }
            // The standard catalog's smallest piece is one cell, and a board always has an empty cell
            // after a placement (a full board would have cleared), so three of them always fit.
            return new[] { _smallest, _smallest, _smallest };
        }

        PieceShape[] DealRaw(Board board, DifficultyBand band, bool helpful)
        {
            var tray = new PieceShape[TraySize];
            for (int slot = 0; slot < TraySize; slot++)
            {
                PieceShape? pick = null;
                if (helpful && _rng.NextInt(100) < _settings.HelpfulChancePercent)
                    pick = PickWeighted(band, Filter.CanClearLine, board);
                tray[slot] = pick ?? PickWeighted(band, Filter.None, board) ?? _smallest;
            }
            return tray;
        }

        PieceShape[] EnsureOneFits(Board board, PieceShape[] tray, DifficultyBand band)
        {
            foreach (var piece in tray)
            {
                if (piece.FitsAnywhere(board)) return tray;
            }

            PieceShape? replacement = PickWeighted(band, Filter.FitsAnywhere, board) ?? PickAnyThatFits(board);
            if (replacement != null) tray[_rng.NextInt(TraySize)] = replacement;
            return tray;
        }

        PieceShape? PickAnyThatFits(Board board)
        {
            var candidates = _allowed[0];
            candidates.Clear();
            foreach (var group in _byCategory)
            {
                foreach (var shape in group)
                {
                    if (shape.FitsAnywhere(board)) candidates.Add(shape);
                }
            }
            return candidates.Count == 0 ? null : candidates[_rng.NextInt(candidates.Count)];
        }

        /// <summary>
        /// Picks a category by weight (among categories with at least one allowed shape), then a shape
        /// in it uniformly. Integer math only, so every platform makes the same picks.
        /// </summary>
        PieceShape? PickWeighted(DifficultyBand band, Filter filter, Board board)
        {
            int total = 0;
            for (int c = 0; c < CategoryCount; c++)
            {
                var allowed = _allowed[c];
                allowed.Clear();
                int weight = band.WeightOf((PieceCategory)c);
                if (weight <= 0) continue;
                foreach (var shape in _byCategory[c])
                {
                    if (Allows(filter, shape, board)) allowed.Add(shape);
                }
                if (allowed.Count > 0) total += weight;
            }
            if (total == 0) return null;

            int roll = _rng.NextInt(total);
            for (int c = 0; c < CategoryCount; c++)
            {
                var allowed = _allowed[c];
                if (allowed.Count == 0) continue;
                int weight = band.WeightOf((PieceCategory)c);
                if (roll < weight) return allowed[_rng.NextInt(allowed.Count)];
                roll -= weight;
            }
            return null;
        }

        static bool Allows(Filter filter, PieceShape shape, Board board)
        {
            switch (filter)
            {
                case Filter.FitsAnywhere: return shape.FitsAnywhere(board);
                case Filter.CanClearLine: return shape.CanClearLine(board);
                default: return true;
            }
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 58`.

- [ ] **Step 6: Commit**

```bash
git add core
git commit -m "Add the piece generator with difficulty bands, mercy and fit guarantees" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Game session and save/resume

**Files:**
- Create: `core/src/GameSnapshot.cs`, `core/src/GameSession.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/GameSessionTests.cs`

**Interfaces:**
- Consumes:
  - `PieceGenerator` (`Deal`, `DealRescue`, `Rng`, `TraySize`)
  - `Scoring.Apply`, `ScoreState`, `ScoringSettings`
  - `GeneratorSettings` (`MercyAfterQuickLosses`)
  - `Pcg32.FromState`
  - `PieceCatalog` indexer
  - `Board`, `PlaceResult`
- Produces:
  - `[Serializable] sealed class GameSnapshot`, with public fields:
    - `Version`, `Board`, `Tray` (`string[]`; `""` = used slot)
    - `Score`, `Streak`, `MissesSinceClear`, `MaxStreak`, `Placements`, `LinesCleared`
    - `RngState`, `RngIncrement`, `GameIndex`, `RecentQuickLosses`, `Revived`
    - `const int CurrentVersion = 1`
  - `readonly struct PlacementOutcome`: `Points`, `Lines`, `ClearedMask`, `BoardCleared`, `Streak`, `NewTrayDealt`, `GameOver`
  - `sealed class GameSession`:
    - constructor `GameSession(PieceCatalog, GeneratorSettings, ScoringSettings, ulong seed, int gameIndex = 0, int recentQuickLosses = 0)`
    - state: `Board Board`, `ReadOnlySpan<PieceShape?> Tray`, `int Score`, `Streak`, `MaxStreak`, `Placements`, `LinesCleared`, `GameIndex`, `RecentQuickLosses`
    - flags: `bool IsMercyGame`, `Revived`, `IsGameOver`
    - moves: `bool CanPlace(int slot, int row, int col)`, `bool TryPlace(int slot, int row, int col, out PlacementOutcome)`, `bool TryRevive()`
    - save/resume: `GameSnapshot ToSnapshot()`, `static GameSession FromSnapshot(GameSnapshot, PieceCatalog, GeneratorSettings, ScoringSettings)`

- [ ] **Step 1: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/GameSessionTests.cs`:
```csharp
using System.Text.Json;

namespace BlockPuzzle.Core.Tests;

public class GameSessionTests
{
    static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    static GameSession NewGame(ulong seed = 1, int recentQuickLosses = 0) =>
        new(PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings(), seed, gameIndex: 0, recentQuickLosses);

    /// <summary>A game in a chosen position, built through a snapshot.</summary>
    static GameSession Position(Board board, string[] tray, bool revived = false)
    {
        var rng = new Pcg32(99);
        var snapshot = new GameSnapshot
        {
            Board = board.Cells,
            Tray = tray,
            RngState = rng.State,
            RngIncrement = rng.Increment,
            Revived = revived,
        };
        return GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings());
    }

    static string TrayIds(GameSession game) => string.Join(",", game.Tray.ToArray().Select(p => p?.Id ?? "-"));

    static void PlayFirstLegalMove(GameSession game)
    {
        var tray = game.Tray.ToArray();
        for (int slot = 0; slot < tray.Length; slot++)
        {
            var shape = tray[slot];
            if (shape is null) continue;
            for (int i = 0; i < shape.PlacementCount; i++)
            {
                if (game.TryPlace(slot, shape.AnchorRow(i), shape.AnchorCol(i), out _)) return;
            }
        }
    }

    [Fact]
    public void A_new_game_starts_empty_with_three_pieces()
    {
        var game = NewGame();

        Assert.True(game.Board.IsEmpty);
        Assert.Equal(3, game.Tray.Length);
        Assert.All(game.Tray.ToArray(), p => Assert.NotNull(p));
        Assert.False(game.IsGameOver);
        Assert.Equal(0, game.Score);
    }

    [Fact]
    public void Invalid_placements_are_rejected()
    {
        var game = Position(new Board(TestBoards.Cell(0, 0)), new[] { "dot", "dot", "dot" });

        Assert.False(game.TryPlace(-1, 1, 1, out _));   // no such slot
        Assert.False(game.TryPlace(3, 1, 1, out _));
        Assert.False(game.TryPlace(0, 8, 0, out _));    // off the board
        Assert.False(game.TryPlace(0, 0, 0, out _));    // overlaps a filled cell
        Assert.True(game.CanPlace(0, 1, 1));
        Assert.True(game.TryPlace(0, 1, 1, out _));
        Assert.False(game.TryPlace(0, 2, 2, out _));    // slot already used
        Assert.Equal(1, game.Placements);
    }

    [Fact]
    public void Placing_the_last_piece_deals_a_new_tray()
    {
        var game = Position(Board.Empty, new[] { "dot", "dot", "dot" });

        Assert.True(game.TryPlace(0, 0, 0, out var first));
        Assert.True(game.TryPlace(1, 0, 2, out _));
        Assert.True(game.TryPlace(2, 0, 4, out var third));

        Assert.False(first.NewTrayDealt);
        Assert.True(third.NewTrayDealt);
        Assert.All(game.Tray.ToArray(), p => Assert.NotNull(p));
    }

    [Fact]
    public void Clearing_a_line_scores_it_and_empties_it()
    {
        var board = Board.Parse(
            "#######.",
            "#.......",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........");
        var game = Position(board, new[] { "dot", "i2h", "i2h" });

        Assert.True(game.TryPlace(0, 0, 7, out var outcome));

        Assert.Equal(1, outcome.Lines);
        Assert.Equal(1 + 10, outcome.Points);
        Assert.False(outcome.BoardCleared);
        Assert.Equal(new Board(TestBoards.Cell(1, 0)), game.Board);
        Assert.Equal(11, game.Score);
        Assert.Equal(1, game.LinesCleared);
    }

    [Fact]
    public void The_game_is_over_when_no_piece_fits()
    {
        var game = Position(TestBoards.Checkerboard, new[] { "i2h", "o3", "o3" });

        Assert.True(game.IsGameOver);
        Assert.False(game.TryPlace(0, 0, 1, out _));
    }

    [Fact]
    public void A_placement_can_end_the_game()
    {
        var board = Board.Parse(
            "#...#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#",
            "#.#.#.#.",
            ".#.#.#.#");
        var game = Position(board, new[] { "i2h", "o3", "o3" });
        Assert.False(game.IsGameOver);

        Assert.True(game.TryPlace(0, 0, 1, out var outcome));

        Assert.True(outcome.GameOver);
        Assert.True(game.IsGameOver);
    }

    [Fact]
    public void A_revive_deals_pieces_that_fit()
    {
        var game = Position(TestBoards.Checkerboard, new[] { "i2h", "o3", "o3" });

        Assert.True(game.TryRevive());

        Assert.False(game.IsGameOver);
        Assert.True(game.Revived);
        Assert.True(TraySolver.CanPlaceAll(game.Board, game.Tray.ToArray()));
        Assert.False(game.TryRevive());   // the game isn't over
    }

    [Fact]
    public void A_second_revive_is_refused()
    {
        var game = Position(TestBoards.Checkerboard, new[] { "i2h", "o3", "o3" }, revived: true);

        Assert.True(game.IsGameOver);
        Assert.False(game.TryRevive());
    }

    [Fact]
    public void Two_quick_losses_in_a_row_make_a_mercy_game()
    {
        Assert.False(NewGame(recentQuickLosses: 1).IsMercyGame);
        Assert.True(NewGame(recentQuickLosses: 2).IsMercyGame);
    }

    [Fact]
    public void A_saved_game_resumes_exactly()
    {
        var original = NewGame(seed: 42);
        for (int i = 0; i < 10 && !original.IsGameOver; i++) PlayFirstLegalMove(original);

        string json = JsonSerializer.Serialize(original.ToSnapshot(), Json);
        var snapshot = JsonSerializer.Deserialize<GameSnapshot>(json, Json)!;
        var restored = GameSession.FromSnapshot(snapshot, PieceCatalog.Standard, GeneratorSettings.Baseline(), new ScoringSettings());

        for (int i = 0; i < 40 && !original.IsGameOver; i++)
        {
            Assert.Equal(TrayIds(original), TrayIds(restored));
            PlayFirstLegalMove(original);
            PlayFirstLegalMove(restored);
            Assert.Equal(original.Board, restored.Board);
            Assert.Equal(original.Score, restored.Score);
            Assert.Equal(original.IsGameOver, restored.IsGameOver);
        }
        Assert.Equal(TrayIds(original), TrayIds(restored));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'GameSession' could not be found`.

- [ ] **Step 3: Implement the snapshot and the session**

`core/src/GameSnapshot.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// Everything needed to resume a game exactly, including the random generator's position.
    /// Public fields so both Unity's JsonUtility and System.Text.Json (with IncludeFields) can save it.
    /// </summary>
    [Serializable]
    public sealed class GameSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public ulong Board;
        /// <summary>Shape id per tray slot; an empty string means the slot was already used.</summary>
        public string[] Tray = Array.Empty<string>();
        public int Score;
        public int Streak;
        public int MissesSinceClear;
        public int MaxStreak;
        public int Placements;
        public int LinesCleared;
        public ulong RngState;
        public ulong RngIncrement;
        public int GameIndex;
        public int RecentQuickLosses;
        public bool Revived;
    }
}
```

`core/src/GameSession.cs`:
```csharp
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace BlockPuzzle.Core
{
    /// <summary>The result of one successful placement.</summary>
    public readonly struct PlacementOutcome
    {
        public readonly int Points;
        public readonly int Lines;
        public readonly ulong ClearedMask;
        public readonly bool BoardCleared;
        public readonly int Streak;
        public readonly bool NewTrayDealt;
        public readonly bool GameOver;

        public PlacementOutcome(int points, int lines, ulong clearedMask, bool boardCleared, int streak, bool newTrayDealt, bool gameOver)
        {
            Points = points;
            Lines = lines;
            ClearedMask = clearedMask;
            BoardCleared = boardCleared;
            Streak = streak;
            NewTrayDealt = newTrayDealt;
            GameOver = gameOver;
        }
    }

    /// <summary>One game: the board, a tray of up to three pieces, scoring and game over.</summary>
    public sealed class GameSession
    {
        readonly GeneratorSettings _generatorSettings;
        readonly ScoringSettings _scoringSettings;
        readonly PieceGenerator _generator;
        readonly PieceShape?[] _tray = new PieceShape?[PieceGenerator.TraySize];
        ScoreState _score;

        /// <summary>Starts a new game.</summary>
        /// <param name="gameIndex">How many games this player started before this one.</param>
        /// <param name="recentQuickLosses">Quick losses in a row just before this game; enough of them makes it a mercy game.</param>
        public GameSession(PieceCatalog catalog, GeneratorSettings generatorSettings, ScoringSettings scoringSettings, ulong seed, int gameIndex = 0, int recentQuickLosses = 0)
            : this(catalog, generatorSettings, scoringSettings, new Pcg32(seed), gameIndex, recentQuickLosses)
        {
            DealTray();
            UpdateGameOver();
        }

        GameSession(PieceCatalog catalog, GeneratorSettings generatorSettings, ScoringSettings scoringSettings, Pcg32 rng, int gameIndex, int recentQuickLosses)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _generatorSettings = generatorSettings ?? throw new ArgumentNullException(nameof(generatorSettings));
            _scoringSettings = scoringSettings ?? throw new ArgumentNullException(nameof(scoringSettings));
            _generator = new PieceGenerator(catalog, generatorSettings, rng);
            GameIndex = gameIndex;
            RecentQuickLosses = recentQuickLosses;
        }

        public Board Board { get; private set; }
        /// <summary>Up to three pieces; null where a piece was already placed.</summary>
        public ReadOnlySpan<PieceShape?> Tray => _tray;
        public int Score => _score.Score;
        public int Streak => _score.Streak;
        public int MaxStreak => _score.MaxStreak;
        public int Placements { get; private set; }
        public int LinesCleared { get; private set; }
        public int GameIndex { get; }
        public int RecentQuickLosses { get; }
        public bool IsMercyGame =>
            _generatorSettings.MercyAfterQuickLosses > 0 && RecentQuickLosses >= _generatorSettings.MercyAfterQuickLosses;
        public bool Revived { get; private set; }
        public bool IsGameOver { get; private set; }

        public bool CanPlace(int slot, int row, int col) => !IsGameOver && TryGetPlacement(slot, row, col, out _, out _);

        /// <summary>Places the piece in a tray slot with its top-left corner at (row, col).</summary>
        public bool TryPlace(int slot, int row, int col, out PlacementOutcome outcome)
        {
            outcome = default;
            if (IsGameOver || !TryGetPlacement(slot, row, col, out var shape, out ulong mask)) return false;

            PlaceResult result = Board.Place(mask);
            Board = result.Board;
            _tray[slot] = null;
            Placements++;
            LinesCleared += result.Lines;
            _score = Scoring.Apply(_scoringSettings, _score, shape.CellCount, result.Lines, result.BoardCleared, out int points);

            bool dealt = false;
            if (TrayIsEmpty())
            {
                DealTray();
                dealt = true;
            }
            UpdateGameOver();

            outcome = new PlacementOutcome(points, result.Lines, result.ClearedMask, result.BoardCleared, _score.Streak, dealt, IsGameOver);
            return true;
        }

        /// <summary>The rewarded revive: once per game, after game over, deals small pieces that all fit.</summary>
        public bool TryRevive()
        {
            if (!IsGameOver || Revived) return false;
            var rescue = _generator.DealRescue(Board);
            for (int i = 0; i < _tray.Length; i++) _tray[i] = rescue[i];
            Revived = true;
            UpdateGameOver();
            return true;
        }

        public GameSnapshot ToSnapshot()
        {
            var tray = new string[_tray.Length];
            for (int i = 0; i < _tray.Length; i++) tray[i] = _tray[i]?.Id ?? "";
            return new GameSnapshot
            {
                Board = Board.Cells,
                Tray = tray,
                Score = _score.Score,
                Streak = _score.Streak,
                MissesSinceClear = _score.MissesSinceClear,
                MaxStreak = _score.MaxStreak,
                Placements = Placements,
                LinesCleared = LinesCleared,
                RngState = _generator.Rng.State,
                RngIncrement = _generator.Rng.Increment,
                GameIndex = GameIndex,
                RecentQuickLosses = RecentQuickLosses,
                Revived = Revived,
            };
        }

        /// <summary>Resumes a saved game exactly, including the pieces it will deal next.</summary>
        public static GameSession FromSnapshot(GameSnapshot snapshot, PieceCatalog catalog, GeneratorSettings generatorSettings, ScoringSettings scoringSettings)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Version != GameSnapshot.CurrentVersion)
                throw new NotSupportedException($"Snapshot version {snapshot.Version} is not supported.");
            if (snapshot.Tray == null || snapshot.Tray.Length != PieceGenerator.TraySize)
                throw new ArgumentException("The snapshot must have three tray slots.", nameof(snapshot));

            var rng = Pcg32.FromState(snapshot.RngState, snapshot.RngIncrement);
            var session = new GameSession(catalog, generatorSettings, scoringSettings, rng, snapshot.GameIndex, snapshot.RecentQuickLosses)
            {
                Board = new Board(snapshot.Board),
                Placements = snapshot.Placements,
                LinesCleared = snapshot.LinesCleared,
                Revived = snapshot.Revived,
            };
            session._score = new ScoreState(snapshot.Score, snapshot.Streak, snapshot.MissesSinceClear, snapshot.MaxStreak);
            for (int i = 0; i < session._tray.Length; i++)
            {
                string id = snapshot.Tray[i];
                session._tray[i] = string.IsNullOrEmpty(id) ? null : catalog[id];
            }
            if (session.TrayIsEmpty()) session.DealTray(); // a saved game always has a piece; recover if not
            session.UpdateGameOver();
            return session;
        }

        bool TryGetPlacement(int slot, int row, int col, [NotNullWhen(true)] out PieceShape? shape, out ulong mask)
        {
            mask = 0;
            shape = (uint)slot < (uint)_tray.Length ? _tray[slot] : null;
            return shape != null && shape.TryGetMask(row, col, out mask) && Board.Fits(mask);
        }

        bool TrayIsEmpty()
        {
            foreach (var piece in _tray)
            {
                if (piece != null) return false;
            }
            return true;
        }

        void DealTray()
        {
            var dealt = _generator.Deal(Board, Placements, IsMercyGame);
            for (int i = 0; i < _tray.Length; i++) _tray[i] = dealt[i];
        }

        void UpdateGameOver()
        {
            foreach (var piece in _tray)
            {
                if (piece != null && piece.FitsAnywhere(Board))
                {
                    IsGameOver = false;
                    return;
                }
            }
            IsGameOver = true;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 68`.

- [ ] **Step 5: Commit**

```bash
git add core
git commit -m "Add the game session with revive and exact save/resume" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Ad policy

**Files:**
- Create: `core/src/AdPolicySettings.cs`, `core/src/AdPolicy.cs`
- Test: `core/dotnet/BlockPuzzle.Core.Tests/AdPolicyTests.cs`

**Interfaces:**
- Produces:
  - `[Serializable] sealed class AdPolicySettings`, with public fields `InterstitialsEnabled = true`, `GraceGames = 3`, `CooldownSeconds = 90`
  - `static class AdPolicy`: `bool CanShowInterstitial(AdPolicySettings settings, bool ownsRemoveAds, int gamesCompleted, double? secondsSinceLastFullScreenAd, bool betweenGames)`

- [ ] **Step 1: Write the failing tests**

`core/dotnet/BlockPuzzle.Core.Tests/AdPolicyTests.cs`:
```csharp
namespace BlockPuzzle.Core.Tests;

public class AdPolicyTests
{
    static readonly AdPolicySettings Defaults = new();

    [Fact]
    public void No_interstitials_during_the_first_three_games()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, ownsRemoveAds: false, gamesCompleted: 2, secondsSinceLastFullScreenAd: null, betweenGames: true));
        Assert.True(AdPolicy.CanShowInterstitial(Defaults, ownsRemoveAds: false, gamesCompleted: 3, secondsSinceLastFullScreenAd: null, betweenGames: true));
    }

    [Fact]
    public void The_cooldown_is_respected()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, false, 10, secondsSinceLastFullScreenAd: 89.9, betweenGames: true));
        Assert.True(AdPolicy.CanShowInterstitial(Defaults, false, 10, secondsSinceLastFullScreenAd: 90, betweenGames: true));
    }

    [Fact]
    public void Never_during_a_game()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, false, 10, null, betweenGames: false));
    }

    [Fact]
    public void Remove_Ads_and_the_kill_switch_block_interstitials()
    {
        Assert.False(AdPolicy.CanShowInterstitial(Defaults, ownsRemoveAds: true, 10, null, true));
        Assert.False(AdPolicy.CanShowInterstitial(new AdPolicySettings { InterstitialsEnabled = false }, false, 10, null, true));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: build FAILS with `error CS0246: The type or namespace name 'AdPolicySettings' could not be found`.

- [ ] **Step 3: Implement the ad policy**

`core/src/AdPolicySettings.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Interstitial rules. Public fields so Remote Config values can be loaded into them.</summary>
    [Serializable]
    public sealed class AdPolicySettings
    {
        /// <summary>Remote kill switch (Remote Config key ads_inter_enabled).</summary>
        public bool InterstitialsEnabled = true;
        /// <summary>No interstitials until a new player has completed this many games (ads_inter_grace_games).</summary>
        public int GraceGames = 3;
        /// <summary>Minimum seconds between full-screen ads (ads_inter_cooldown_s).</summary>
        public int CooldownSeconds = 90;
    }
}
```

`core/src/AdPolicy.cs`:
```csharp
#nullable enable
using System;

namespace BlockPuzzle.Core
{
    /// <summary>Decides when an interstitial may show. Pure logic, so it can be unit-tested.</summary>
    public static class AdPolicy
    {
        /// <param name="secondsSinceLastFullScreenAd">Null if no full-screen ad has been shown yet.</param>
        /// <param name="betweenGames">True only after a game ended, when the player taps Play Again or Home.</param>
        public static bool CanShowInterstitial(AdPolicySettings settings, bool ownsRemoveAds, int gamesCompleted, double? secondsSinceLastFullScreenAd, bool betweenGames)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!settings.InterstitialsEnabled || ownsRemoveAds || !betweenGames) return false;
            if (gamesCompleted < settings.GraceGames) return false;
            return secondsSinceLastFullScreenAd == null || secondsSinceLastFullScreenAd.Value >= settings.CooldownSeconds;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test core/BlockPuzzle.slnx`
Expected: `Passed!  - Failed: 0, Passed: 72`.

- [ ] **Step 5: Commit**

```bash
git add core
git commit -m "Add the interstitial ad policy" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Simulator and generator tuning

**Files:**
- Create:
  - `core/dotnet/BlockPuzzle.Sim/BlockPuzzle.Sim.csproj`
  - `core/dotnet/BlockPuzzle.Sim/SimOptions.cs`
  - `core/dotnet/BlockPuzzle.Sim/Bots.cs`
  - `core/dotnet/BlockPuzzle.Sim/SimulationRunner.cs`
  - `core/dotnet/BlockPuzzle.Sim/Report.cs`
  - `core/dotnet/BlockPuzzle.Sim/Program.cs`
- Modify:
  - `core/BlockPuzzle.slnx` (add the project)
  - `.github/workflows/core.yml` (add a smoke run)
  - `core/src/GeneratorSettings.cs` (tuned preset weights, if the first run misses the targets)

**Interfaces:**
- Consumes:
  - `GameSession` (constructor, `Board`, `Tray`, `TryPlace`, `IsGameOver`, `Placements`, `Score`, `LinesCleared`, `MaxStreak`, `IsMercyGame`)
  - `PieceShape` (`Placements`, `AnchorRow`, `AnchorCol`, `FitsAnywhere`)
  - `PlaceResult`, `GeneratorSettings` (`ForProfile`, `QuickLossPlacements`), `ScoringSettings`, `PieceCatalog.Standard`, `Pcg32`
- Produces: the CLI `dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- [--games N] [--games-per-player N] [--bot greedy|random] [--profile gentle|baseline|hard|all] [--seed N]`

The simulator is a measuring tool, not shipped code. Its check is a real run, not unit tests.

- [ ] **Step 1: Create the project**

`core/dotnet/BlockPuzzle.Sim/BlockPuzzle.Sim.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\BlockPuzzle.Core\BlockPuzzle.Core.csproj" />
  </ItemGroup>

</Project>
```

Run: `dotnet sln core/BlockPuzzle.slnx add core/dotnet/BlockPuzzle.Sim/BlockPuzzle.Sim.csproj`

- [ ] **Step 2: Write the options, bots, runner and report**

`core/dotnet/BlockPuzzle.Sim/SimOptions.cs`:
```csharp
namespace BlockPuzzle.Sim;

/// <summary>Command-line options for the simulator.</summary>
public sealed class SimOptions
{
    static readonly string[] AllProfiles = { "gentle", "baseline", "hard" };

    public int Games { get; private set; } = 20_000;
    public int GamesPerPlayer { get; private set; } = 50;
    public string Bot { get; private set; } = "greedy";
    public IReadOnlyList<string> Profiles { get; private set; } = AllProfiles;
    public ulong Seed { get; private set; } = 1;

    /// <summary>Each simulated player plays GamesPerPlayer games in a row, so quick losses can trigger mercy.</summary>
    public int Players => Math.Max(1, Games / GamesPerPlayer);

    public const string Usage = """
        Usage: dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- [options]

          --games N             total games per profile (default 20000)
          --games-per-player N  games each simulated player plays in a row (default 50)
          --bot greedy|random   who plays (default greedy)
          --profile NAME        gentle, baseline, hard or all (default all)
          --seed N              base seed (default 1)
        """;

    /// <summary>Returns null for --help; throws ArgumentException or FormatException for bad input.</summary>
    public static SimOptions? Parse(string[] args)
    {
        var options = new SimOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");

            switch (args[i])
            {
                case "--games": options.Games = int.Parse(Value()); break;
                case "--games-per-player": options.GamesPerPlayer = int.Parse(Value()); break;
                case "--bot": options.Bot = Value(); break;
                case "--profile":
                    string profile = Value();
                    options.Profiles = profile == "all" ? AllProfiles : new[] { profile };
                    break;
                case "--seed": options.Seed = ulong.Parse(Value()); break;
                case "--help" or "-h": return null;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        if (options.Bot is not ("greedy" or "random")) throw new ArgumentException("--bot must be greedy or random.");
        if (options.Games < 1 || options.GamesPerPlayer < 1) throw new ArgumentException("--games and --games-per-player must be positive.");
        foreach (string profile in options.Profiles)
        {
            if (!AllProfiles.Contains(profile)) throw new ArgumentException($"Unknown profile '{profile}'.");
        }
        return options;
    }
}
```

`core/dotnet/BlockPuzzle.Sim/Bots.cs`:
```csharp
using System.Numerics;
using BlockPuzzle.Core;

namespace BlockPuzzle.Sim;

/// <summary>A player for the simulator.</summary>
public interface IBot
{
    /// <summary>Makes one move; false if no piece fits anywhere.</summary>
    bool PlayOneMove(GameSession game);
}

/// <summary>Picks uniformly among all legal moves: a floor for how hard the generator is.</summary>
public sealed class RandomBot : IBot
{
    readonly Pcg32 _rng;
    readonly List<(int Slot, int Index)> _moves = new();

    public RandomBot(ulong seed) => _rng = new Pcg32(seed, sequence: 7);

    public bool PlayOneMove(GameSession game)
    {
        _moves.Clear();
        var board = game.Board;
        var tray = game.Tray;
        for (int slot = 0; slot < tray.Length; slot++)
        {
            var shape = tray[slot];
            if (shape is null) continue;
            var placements = shape.Placements;
            for (int i = 0; i < placements.Length; i++)
            {
                if (board.Fits(placements[i])) _moves.Add((slot, i));
            }
        }
        if (_moves.Count == 0) return false;

        var (chosenSlot, chosenIndex) = _moves[_rng.NextInt(_moves.Count)];
        var chosen = tray[chosenSlot]!;
        return game.TryPlace(chosenSlot, chosen.AnchorRow(chosenIndex), chosen.AnchorCol(chosenIndex), out _);
    }
}

/// <summary>
/// Looks one move ahead: clear lines first, then keep the board smooth, avoid single-cell holes
/// and keep room for the big pieces. A stand-in for a decent human player.
/// </summary>
public sealed class GreedyBot : IBot
{
    const ulong NotColumn0 = 0xFEFEFEFEFEFEFEFEUL;
    const ulong NotColumn7 = 0x7F7F7F7F7F7F7F7FUL;
    const ulong NotRow7 = 0x00FFFFFFFFFFFFFFUL;

    static readonly PieceShape Square3 = PieceCatalog.Standard["o3"];
    static readonly PieceShape Line5Horizontal = PieceCatalog.Standard["i5h"];
    static readonly PieceShape Line5Vertical = PieceCatalog.Standard["i5v"];

    public bool PlayOneMove(GameSession game)
    {
        var board = game.Board;
        var tray = game.Tray;
        int bestSlot = -1, bestIndex = -1, bestValue = int.MinValue;
        for (int slot = 0; slot < tray.Length; slot++)
        {
            var shape = tray[slot];
            if (shape is null) continue;
            var placements = shape.Placements;
            for (int i = 0; i < placements.Length; i++)
            {
                if (!board.Fits(placements[i])) continue;
                int value = Evaluate(board.Place(placements[i]));
                if (value > bestValue)
                {
                    bestValue = value;
                    bestSlot = slot;
                    bestIndex = i;
                }
            }
        }
        if (bestSlot < 0) return false;

        var chosen = tray[bestSlot]!;
        return game.TryPlace(bestSlot, chosen.AnchorRow(bestIndex), chosen.AnchorCol(bestIndex), out _);
    }

    static int Evaluate(PlaceResult result)
    {
        ulong filled = result.Board.Cells;
        int value = result.Lines * 1000 + (result.BoardCleared ? 3000 : 0);
        value -= 6 * Roughness(filled);
        value -= 40 * BitOperations.PopCount(IsolatedEmpty(filled));
        if (Square3.FitsAnywhere(result.Board)) value += 40;
        if (Line5Horizontal.FitsAnywhere(result.Board)) value += 20;
        if (Line5Vertical.FitsAnywhere(result.Board)) value += 20;
        return value;
    }

    /// <summary>How many neighbouring cell pairs differ (one filled, one empty): lower is smoother.</summary>
    static int Roughness(ulong filled) =>
        BitOperations.PopCount((filled ^ (filled >> 1)) & NotColumn7) +
        BitOperations.PopCount((filled ^ (filled >> 8)) & NotRow7);

    /// <summary>Empty cells whose four neighbours are all filled or off the board.</summary>
    static ulong IsolatedEmpty(ulong filled)
    {
        ulong empty = ~filled;
        ulong emptyLeft = (empty << 1) & NotColumn0;
        ulong emptyRight = (empty >> 1) & NotColumn7;
        ulong emptyAbove = empty << 8;
        ulong emptyBelow = empty >> 8;
        return empty & ~(emptyLeft | emptyRight | emptyAbove | emptyBelow);
    }
}
```

`core/dotnet/BlockPuzzle.Sim/SimulationRunner.cs`:
```csharp
using BlockPuzzle.Core;

namespace BlockPuzzle.Sim;

public readonly record struct GameResult(int Placements, int Score, int Lines, int MaxStreak, bool Mercy, bool Capped);

/// <summary>Plays many games in parallel. Deterministic: every player and game has its own derived seed.</summary>
public static class SimulationRunner
{
    /// <summary>Stops endless games; a high share of capped games means the profile is far too easy.</summary>
    public const int PlacementCap = 10_000;

    public static GameResult[] Run(PieceCatalog catalog, GeneratorSettings generator, ScoringSettings scoring, string bot, int players, int gamesPerPlayer, ulong seed)
    {
        var results = new GameResult[players * gamesPerPlayer];
        Parallel.For(0, players, player =>
        {
            IBot playerBot = bot == "random" ? new RandomBot(Mix(seed, (ulong)player, 0xB07)) : new GreedyBot();
            int quickLosses = 0;
            for (int g = 0; g < gamesPerPlayer; g++)
            {
                var game = new GameSession(catalog, generator, scoring, Mix(seed, (ulong)player, (ulong)g), g, quickLosses);
                bool capped = false;
                while (!game.IsGameOver)
                {
                    if (game.Placements >= PlacementCap)
                    {
                        capped = true;
                        break;
                    }
                    if (!playerBot.PlayOneMove(game)) break;
                }

                results[player * gamesPerPlayer + g] = new GameResult(game.Placements, game.Score, game.LinesCleared, game.MaxStreak, game.IsMercyGame, capped);
                quickLosses = game.Placements < generator.QuickLossPlacements ? quickLosses + 1 : 0;
            }
        });
        return results;
    }

    /// <summary>SplitMix64 finalizer over the inputs: well-spread, reproducible seeds.</summary>
    static ulong Mix(ulong a, ulong b, ulong c)
    {
        ulong z = a ^ (b * 0x9E3779B97F4A7C15UL) ^ (c * 0xC2B2AE3D27D4EB4FUL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
```

`core/dotnet/BlockPuzzle.Sim/Report.cs`:
```csharp
namespace BlockPuzzle.Sim;

/// <summary>Formats one table row per profile.</summary>
public static class Report
{
    public static string Header =>
        $"{"profile",-10}{"bot",-8}{"games",8}{"median",8}{"p10",6}{"p90",7}{"mean",8}{"<20",8}{"lines",8}{"score",9}{"streak90",10}{"mercy",8}{"capped",8}{"secs",7}";

    public static string Row(string profile, string bot, GameResult[] results, TimeSpan elapsed)
    {
        int[] placements = results.Select(r => r.Placements).Order().ToArray();
        int[] scores = results.Select(r => r.Score).Order().ToArray();
        int[] streaks = results.Select(r => r.MaxStreak).Order().ToArray();

        return $"{profile,-10}{bot,-8}{results.Length,8}" +
               $"{Percentile(placements, 50),8}{Percentile(placements, 10),6}{Percentile(placements, 90),7}{placements.Average(),8:F1}" +
               $"{Percent(results, r => r.Placements < 20),7:F1}%{results.Average(r => r.Lines),8:F1}" +
               $"{Percentile(scores, 50),9}{Percentile(streaks, 90),10}" +
               $"{Percent(results, r => r.Mercy),7:F1}%{Percent(results, r => r.Capped),7:F1}%{elapsed.TotalSeconds,7:F1}";
    }

    static int Percentile(int[] sorted, int percent) => sorted[(int)Math.Round((sorted.Length - 1) * percent / 100.0)];

    static double Percent(GameResult[] results, Func<GameResult, bool> predicate) =>
        100.0 * results.Count(predicate) / results.Length;
}
```

`core/dotnet/BlockPuzzle.Sim/Program.cs`:
```csharp
using System.Diagnostics;
using BlockPuzzle.Core;
using BlockPuzzle.Sim;

SimOptions? options;
try
{
    options = SimOptions.Parse(args);
}
catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine(SimOptions.Usage);
    return 1;
}
if (options is null)
{
    Console.WriteLine(SimOptions.Usage);
    return 0;
}

Console.WriteLine($"{options.Players * options.GamesPerPlayer:N0} games per profile " +
                  $"({options.Players:N0} players x {options.GamesPerPlayer} games), bot: {options.Bot}, seed: {options.Seed}");
Console.WriteLine();
Console.WriteLine(Report.Header);

foreach (string profile in options.Profiles)
{
    var settings = GeneratorSettings.ForProfile(profile);
    var stopwatch = Stopwatch.StartNew();
    var results = SimulationRunner.Run(PieceCatalog.Standard, settings, new ScoringSettings(), options.Bot, options.Players, options.GamesPerPlayer, options.Seed);
    Console.WriteLine(Report.Row(profile, options.Bot, results, stopwatch.Elapsed));
}

Console.WriteLine();
Console.WriteLine("Columns are placements per game except: <20 = games ending before placement 20, lines = lines per game,");
Console.WriteLine("score = median score, streak90 = 90th-percentile best streak, mercy = mercy games, capped = games stopped at the cap.");
Console.WriteLine("Target for baseline + greedy: median 80-150 placements, under 5% of games ending before 20.");
return 0;
```

- [ ] **Step 3: Run the simulator**

Run: `dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 20000`
Expected:
- one row each for gentle, baseline and hard, with medians ordered gentle > baseline > hard
- capped games near 0%
- a run time of seconds to a couple of minutes, depending on core count

Also run `--bot random`: random-bot medians should be well below the greedy ones.

- [ ] **Step 4: Tune the presets**

If baseline + greedy misses the target, adjust the `Baseline()` band weights in `core/src/GeneratorSettings.cs` and run Step 3 again:
- Median too low, or too many games under 20: move weight from Large to Small.
- Median too high: move weight from Small to Large.

Keep gentle clearly easier and hard clearly harder. Record the final table in the "Results" section below.

- [ ] **Step 5: Add a simulator smoke run to CI**

Append to the `steps` in `.github/workflows/core.yml`:
```yaml
      - name: Smoke-run the simulator
        run: dotnet run --project core/dotnet/BlockPuzzle.Sim -c Release -- --games 500 --profile baseline
```

- [ ] **Step 6: Run everything once more and commit**

Run: `dotnet test core/BlockPuzzle.slnx -c Release`
Expected: `Passed!  - Failed: 0, Passed: 72`.

```bash
git add core .github docs
git commit -m "Add the simulator and tune the generator presets" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Results

Final run after tuning: 20,000 games per profile (400 simulated players × 50 games), seed 1, greedy bot.

| Profile | Median | P10 | P90 | Mean | Games ending before 20 | Lines per game | Median score | Mercy games |
|---|---|---|---|---|---|---|---|---|
| gentle | 155 | 65 | 335 | 184.0 | 0.0% | 84.9 | 2,976 | 0.0% |
| baseline | 98 | 43 | 196 | 110.5 | 0.1% | 48.8 | 1,593 | 0.0% |
| hard | 46 | 23 | 89 | 52.4 | 4.7% | 23.6 | 760 | 0.1% |

- **Baseline tuning.**
  - Before: small/medium/large weights 35/45/20 → 28/45/27 → 22/43/35 gave a median of 68, too hard for the 80–150 target.
  - Two steps of moving weight from large to small pieces gave 43/45/12 → 37/45/18 → 31/44/25.
  - After: a median of 98. Seed 2 gives 98 too. These are now the `Baseline()` weights in `core/src/GeneratorSettings.cs`. The code block in Task 5 shows the starting values.
- **Random bot:** medians of 19 / 17 / 14 (gentle / baseline / hard). Placement quality matters far more than luck, which is what a skill game needs.
- **Mercy:** almost never triggers for the greedy bot, because it rarely loses quickly. For the random bot it covers 23–60% of games, so it's working.
- **Cap and speed:** no game hit the 10,000-placement cap, and a full 3 × 20,000-game run takes about 3 seconds.
- **Next:** recalibrate against real players in master-plan task 1.9. The target there is Classic games lasting 3–8 minutes.

### Final review

A fresh reviewer checked the whole branch. Verdict: "with fixes", with no critical issues. The three important findings were fixed test-first (85 tests now):

- **Settings validation:**
  - It no longer crashes on a missing band.
  - It rejects weights above 1,000,000, so they can't overflow when added.
  - It caps `MaxWholeTrayAttempts` at 100.
  - A mistyped Remote Config value now fails with `ArgumentException`, which the Phase 3 loader can catch and fall back to defaults.
- **Golden determinism tests** (`DeterminismTests`): they pin the exact trays a fixed seed deals for each profile, and the end state of a seeded game played first-fit. If dealing ever changes on any platform or app version, CI fails.
- **Corrupt saves:** `FromSnapshot` now always fails with `ArgumentException`, for:
  - unknown piece ids
  - another snapshot version
  - negative counters
  - boards with a full row or column (`Board.HasFullLine`)

  Shape ids are documented as a saved format.

Deferred, minor today:
- **Task 1.8 (Unity prototype):**
  - Check that Unity's `JsonUtility` saves and loads the 64-bit snapshot fields exactly.
  - Run the golden tests in EditMode.
  - Commit the `.meta` files Unity generates in `core/src`.
  - Done in the Task 1.8 engine prep (below):
    - Add `GetTrayPiece(slot)`, because the `Tray` span can't be used across coroutine or async boundaries.
    - Move the quick-loss rule into the engine, so the Unity code and the simulator share it.
    - Set `<Nullable>disable</Nullable>` in the engine project, so CI catches a missing `#nullable enable` the way Unity would.
    - Add that directive to `PieceCategory.cs` and `FitGuarantee.cs`.
- **Task 1.9 (playtests):**
  - Re-derive Gentle's weights from the tuned Baseline. Its pieces are no longer smaller on average; it is easier only because of WholeTray and its helpful phase.
  - Decide whether the revive gets its own small-piece band. Today it reuses Mercy, giving 34.5% medium and 4.9% large pieces.
- **Phase 2 (Daily Puzzle):** store the generator settings name and version, plus a fingerprint of the piece catalog, in the snapshot. A save made with different settings or pieces should then be rejected instead of silently dealing differently. Settings are already copied when a game starts.
- **Phase 3 (ads):**
  - Document that `gamesCompleted` includes the game that just ended.
  - Make `TryRevive` not spend the revive when the rescue tray can't be placed. That can only happen with a custom catalog.
- **CI** (done in the review follow-up below): move `actions/checkout` and `actions/setup-dotnet` to their current majors (v4 targets Node 20), and add `permissions: contents: read`.
- **Small:**
  - The simulator's percentile index overflows above about 23.8 million games.
  - A null shape, a null row or an out-of-range category throws `NullReferenceException` or `IndexOutOfRangeException` instead of `ArgumentException`.

### PR review

Before merging, PR #1 got a second review: ten independent reviewers, a separate verifier for every finding, and a last pass for gaps. Of its 15 findings, 14 were fixed test-first. Every new test was checked against a code change that breaks the behavior it protects. There are now 101 tests.
- **Off-board moves:** `TryGetMask` overflowed for coordinates near `int.MaxValue`, so off-board moves were accepted and scored. It now uses an unsigned bounds check.
- **Settings:**
  - Undefined `FitGuarantee` values and negative or oversized scoring values are rejected.
  - A game copies its settings, so later edits can't change it.
  - `new GeneratorSettings()` is the tuned Baseline.
  - The score stops at `int.MaxValue` instead of wrapping negative.
- **Saves:** `FromSnapshot` throws `InvalidSnapshotException` only for a bad save; a settings problem stays an `ArgumentException`. Empty trays and impossible counters are rejected instead of patched.
- **Tests:**
  - Golden deals on crowded boards now pin the fallback deals and the rescue tray.
  - The WholeTray, mercy, revive and save round-trip tests now fail when their behavior breaks.
- **Docs and `.gitignore`:** WholeTray's fallback is documented, and app builds and signing keys are ignored.

Left open from this review:
- **Phase 2:** saves don't record the piece catalog (above).
- **Task 1.8:**
  - `TraySolver` is an unpruned search. On adversarial boards a WholeTray deal can take about 2.4 ms on desktop, which could cost a frame on a low-end phone. Measure it there, and prune if needed.
  - Cache the `CanClearLine` filter per deal, and avoid allocating a tray array on every deal (done in the Task 1.8 engine prep below).
- **Small** (done in the review follow-up below):
  - CI doesn't check the simulator's difficulty target.
  - `--games` is rounded down to a multiple of games per player.
  - Three more tests are missing: that MaxStreak keeps the best streak, that `MercyAfterQuickLosses = 0` turns mercy off, and a board clear during a streak.
  - `PieceCatalog.Shapes` exposes its internal array.
  - Cleanups: profile and bot names are duplicated, the row and column masks are written out by hand in places, and the retry and tray-copy loops are repeated.

### Review follow-up

A follow-up PR fixed the small items above and the CI item from the first review, test-first. Each new test was checked against a code change that breaks it.
- **CI:**
  - Uses `actions/checkout@v7` and `actions/setup-dotnet@v6`, with `permissions: contents: read`.
  - A new step fails the build when the greedy bot on the baseline profile misses the difficulty target (`--check-target`, 20,000 games).
- **Simulator:**
  - `--games` runs exactly that many games; the last player may play fewer.
  - A new `BlockPuzzle.Sim.Tests` project covers the options, the runner, the target check and the bots, and pins a golden GreedyBot game.
- **Tests:**
  - MaxStreak keeps the best streak after a reset.
  - `MercyAfterQuickLosses = 0` turns mercy off.
  - A board clear during a streak gets the flat +300.
- **Catalog:** `PieceCatalog.Shapes` is a read-only wrapper, built once.
- **Cleanups:**
  - `GeneratorSettings.ProfileNames` and `Bots.Names` are now the only lists of profile and bot names.
  - `Board.RowMask` and `Board.ColumnMask` replace the hand-written masks.
  - `PieceGenerator.DealPlaceable` and `GameSession.SetTray` replace the repeated loops.

A fresh review of the follow-up found nothing critical or important. Its five minor findings were fixed too:
- `SimulationRunner.Run` rejects counts below 1, instead of dividing by zero or returning empty results.
- A golden seeded run pins each game's seed and slot, the random bot's seed and the mercy carry-over.
- Boundary tests pin the target's edges: a median of exactly 80 or 150 passes, and exactly 5% of games ending early misses.
- The report header and footnote take the early-end placement from `Report.EarlyEndPlacements`.
- The file table above is current.

The golden values didn't change. Neither did the 20,000-game medians: 155, 98 and 46 placements for gentle, baseline and hard. There are now 147 tests: 110 for the engine and 37 for the simulator.

### Task 1.8 engine prep

The parts of Task 1.8 that don't need Unity went first, test-first, in their own PR. Each new test was checked against a code change that breaks it.
- **`GameSession.GetTrayPiece(slot)`:** reads one tray slot without the `Tray` span, which can't cross coroutine or async boundaries.
- **`GameSession.NextRecentQuickLosses`:** the quick-loss rule now lives in the engine.
  - Pass it as `recentQuickLosses` to the player's next game. The simulator now does the same.
  - It stops at `int.MaxValue` instead of wrapping.
- **Nullable:** the engine project turns nullable off, as Unity does, and every file opts in with `#nullable enable`. A file that forgets it fails the build with CS8632, where Unity would only warn.
- **Dealing:**
  - `PieceGenerator.Deal` and `DealRescue` gain overloads that fill a three-slot tray.
  - `GameSession` deals straight into its own tray, so a game allocates nothing per deal. A test checks this with `GC.GetAllocatedBytesForCurrentThread`.
  - `CanClearLine` is worked out once per board instead of on every helpful pick. A desktop benchmark measured helpful-phase deals on a board that changes every deal: they are 1.15–2.7 times faster. For example, baseline on an empty board went from 18.9 to 7.1 µs per deal.

The golden values and the 20,000-game medians (155, 98 and 46) didn't change. There are now 160 tests: 123 for the engine and 37 for the simulator.

Still open for Task 1.8; each needs Unity or a phone:
- Create the Unity 6.3 LTS project and build the prototype itself (the master plan's 1.8 list).
- Check that `JsonUtility` saves and loads the 64-bit snapshot fields exactly.
- Run the golden tests in EditMode.
- Commit the `.meta` files Unity generates in `core/src`.
- Measure `TraySolver` on a low-end Android phone, and prune it if needed.
