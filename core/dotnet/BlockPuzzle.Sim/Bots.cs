using System.Numerics;
using BlockPuzzle.Core;

namespace BlockPuzzle.Sim;

/// <summary>A player for the simulator.</summary>
public interface IBot
{
    /// <summary>Makes one move; false if no piece fits anywhere.</summary>
    bool PlayOneMove(GameSession game);
}

/// <summary>The bots the simulator can run, by name.</summary>
public static class Bots
{
    static readonly (string Name, Func<ulong, IBot> Create)[] All =
    {
        ("greedy", _ => new GreedyBot()),
        ("random", seed => new RandomBot(seed)),
    };

    /// <summary>The names --bot accepts.</summary>
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(Array.ConvertAll(All, bot => bot.Name));

    /// <summary>Creates the named bot. Only bots that make random choices use the seed.</summary>
    public static IBot Create(string name, ulong seed)
    {
        foreach (var bot in All)
        {
            if (bot.Name == name) return bot.Create(seed);
        }
        throw new ArgumentException($"Unknown bot '{name}'.", nameof(name));
    }
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
    static readonly ulong NotFirstColumn = ~Board.ColumnMask(0);
    static readonly ulong NotLastColumn = ~Board.ColumnMask(Board.Size - 1);
    static readonly ulong NotLastRow = ~Board.RowMask(Board.Size - 1);

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
        BitOperations.PopCount((filled ^ (filled >> 1)) & NotLastColumn) +
        BitOperations.PopCount((filled ^ (filled >> Board.Size)) & NotLastRow);

    /// <summary>Empty cells whose four neighbours are all filled or off the board.</summary>
    static ulong IsolatedEmpty(ulong filled)
    {
        ulong empty = ~filled;
        ulong emptyLeft = (empty << 1) & NotFirstColumn;
        ulong emptyRight = (empty >> 1) & NotLastColumn;
        ulong emptyAbove = empty << Board.Size;
        ulong emptyBelow = empty >> Board.Size;
        return empty & ~(emptyLeft | emptyRight | emptyAbove | emptyBelow);
    }
}
