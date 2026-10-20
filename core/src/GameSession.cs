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
            if (generatorSettings == null) throw new ArgumentNullException(nameof(generatorSettings));
            if (scoringSettings == null) throw new ArgumentNullException(nameof(scoringSettings));
            if (gameIndex < 0) throw new ArgumentOutOfRangeException(nameof(gameIndex));
            if (recentQuickLosses < 0) throw new ArgumentOutOfRangeException(nameof(recentQuickLosses));

            // Copies, so later edits to the caller's settings objects can't change a game in progress.
            _generatorSettings = generatorSettings.Clone();
            _scoringSettings = scoringSettings.Clone();
            _scoringSettings.Validate();
            _generator = new PieceGenerator(catalog, _generatorSettings, rng);
            GameIndex = gameIndex;
            RecentQuickLosses = recentQuickLosses;
        }

        public Board Board { get; private set; }
        /// <summary>Up to three pieces; null where a piece was already placed.</summary>
        public ReadOnlySpan<PieceShape?> Tray => _tray;

        /// <summary>The piece in a tray slot, or null once it was placed. Unlike Tray, it can be used in coroutines and async code.</summary>
        public PieceShape? GetTrayPiece(int slot)
        {
            if ((uint)slot >= (uint)_tray.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            return _tray[slot];
        }

        public int Score => _score.Score;
        public int Streak => _score.Streak;
        public int MaxStreak => _score.MaxStreak;
        public int Placements { get; private set; }
        public int LinesCleared { get; private set; }
        public int GameIndex { get; }
        public int RecentQuickLosses { get; }
        public bool IsMercyGame =>
            _generatorSettings.MercyAfterQuickLosses > 0 && RecentQuickLosses >= _generatorSettings.MercyAfterQuickLosses;
        /// <summary>
        /// What to pass as recentQuickLosses to this player's next game, once this one has ended: one more than
        /// RecentQuickLosses if it ended before GeneratorSettings.QuickLossPlacements placements, otherwise 0.
        /// </summary>
        public int NextRecentQuickLosses =>
            Placements >= _generatorSettings.QuickLossPlacements ? 0 :
            RecentQuickLosses == int.MaxValue ? int.MaxValue : RecentQuickLosses + 1;
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
            _generator.DealRescue(Board, _tray);
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
        /// <exception cref="InvalidSnapshotException">
        /// The save can't be resumed: it is from another version, corrupt (a negative or impossible counter, a full
        /// row or column, an empty or broken tray, a broken generator state) or names a piece the catalog doesn't
        /// have. Callers can catch this and start a new game.
        /// </exception>
        /// <exception cref="ArgumentException">The settings or catalog passed in are invalid; the save itself may be fine.</exception>
        public static GameSession FromSnapshot(GameSnapshot snapshot, PieceCatalog catalog, GeneratorSettings generatorSettings, ScoringSettings scoringSettings)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (generatorSettings == null) throw new ArgumentNullException(nameof(generatorSettings));
            if (scoringSettings == null) throw new ArgumentNullException(nameof(scoringSettings));
            generatorSettings.Validate(); // a settings problem is an ArgumentException, never "a bad save"
            scoringSettings.Validate();

            if (snapshot.Version != GameSnapshot.CurrentVersion)
                throw new InvalidSnapshotException($"Snapshot version {snapshot.Version} is not supported.");
            if (snapshot.Tray == null || snapshot.Tray.Length != PieceGenerator.TraySize)
                throw new InvalidSnapshotException("The snapshot must have three tray slots.");
            if (snapshot.Score < 0 || snapshot.Streak < 0 || snapshot.MissesSinceClear < 0 || snapshot.MaxStreak < 0 ||
                snapshot.Placements < 0 || snapshot.LinesCleared < 0 || snapshot.GameIndex < 0 || snapshot.RecentQuickLosses < 0)
                throw new InvalidSnapshotException("The snapshot has a negative counter.");
            if (snapshot.Streak > snapshot.MaxStreak)
                throw new InvalidSnapshotException("The snapshot's streak is above its best streak.");
            if (snapshot.Placements == 0 && (snapshot.Score > 0 || snapshot.LinesCleared > 0 || snapshot.MaxStreak > 0))
                throw new InvalidSnapshotException("The snapshot has points or clears without any placements.");
            if ((snapshot.RngIncrement & 1UL) == 0)
                throw new InvalidSnapshotException("The snapshot's random generator state is broken.");

            var board = new Board(snapshot.Board);
            if (board.HasFullLine)
                throw new InvalidSnapshotException("The snapshot's board has a full row or column, which play never leaves behind.");

            var tray = new PieceShape?[PieceGenerator.TraySize];
            bool anyPiece = false;
            for (int i = 0; i < tray.Length; i++)
            {
                string id = snapshot.Tray[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (!catalog.TryGet(id, out var shape))
                    throw new InvalidSnapshotException($"The snapshot names an unknown piece '{id}'.");
                tray[i] = shape;
                anyPiece = true;
            }
            if (!anyPiece)
                throw new InvalidSnapshotException("The snapshot's tray is empty, which play never leaves behind.");

            var rng = Pcg32.FromState(snapshot.RngState, snapshot.RngIncrement);
            var session = new GameSession(catalog, generatorSettings, scoringSettings, rng, snapshot.GameIndex, snapshot.RecentQuickLosses)
            {
                Board = board,
                Placements = snapshot.Placements,
                LinesCleared = snapshot.LinesCleared,
                Revived = snapshot.Revived,
            };
            session._score = new ScoreState(snapshot.Score, snapshot.Streak, snapshot.MissesSinceClear, snapshot.MaxStreak);
            Array.Copy(tray, session._tray, tray.Length);
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

        void DealTray() => _generator.Deal(Board, Placements, IsMercyGame, _tray);

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
