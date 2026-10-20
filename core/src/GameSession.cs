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
