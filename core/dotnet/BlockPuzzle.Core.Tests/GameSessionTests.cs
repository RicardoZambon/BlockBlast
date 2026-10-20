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
