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
        // CanClearLine for every shape (by category, like _byCategory) on _canClearLineBoard. The helpful bias asks
        // on every slot and every WholeTray re-deal, and the answers only change with the board.
        readonly bool[][] _canClearLine;
        Board _canClearLineBoard;
        bool _canClearLineKnown;

        public PieceGenerator(PieceCatalog catalog, GeneratorSettings settings, Pcg32 rng)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _settings = settings.Clone(); // later edits to the caller's object can't reach this generator
            _settings.Validate();

            var lists = new List<PieceShape>[CategoryCount];
            for (int c = 0; c < CategoryCount; c++) lists[c] = new List<PieceShape>();

            _smallest = catalog.Shapes[0];
            foreach (var shape in catalog.Shapes)
            {
                lists[(int)shape.Category].Add(shape);
                if (shape.CellCount < _smallest.CellCount) _smallest = shape;
            }

            _byCategory = new PieceShape[CategoryCount][];
            _canClearLine = new bool[CategoryCount][];
            _allowed = new List<PieceShape>[CategoryCount];
            for (int c = 0; c < CategoryCount; c++)
            {
                _byCategory[c] = lists[c].ToArray();
                _canClearLine[c] = new bool[_byCategory[c].Length];
                // Room for the whole catalog (PickAnyThatFits fills _allowed[0] from every category), so dealing never grows them.
                _allowed[c] = new List<PieceShape>(catalog.Shapes.Count);
            }
        }

        /// <summary>The random generator, so a session can save and restore its exact position.</summary>
        public Pcg32 Rng => _rng;

        /// <summary>Deals a tray for the board.</summary>
        /// <param name="placements">Placements made so far in this game; picks the difficulty band.</param>
        /// <param name="mercyGame">True if this game started after too many quick losses.</param>
        public PieceShape[] Deal(Board board, int placements, bool mercyGame)
        {
            var tray = new PieceShape[TraySize];
            Deal(board, placements, mercyGame, tray);
            return tray;
        }

        /// <summary>Deals a tray for the board into <paramref name="tray"/>, overwriting all three slots. Allocates nothing.</summary>
        public void Deal(Board board, int placements, bool mercyGame, PieceShape?[] tray)
        {
            CheckTray(tray);
            DifficultyBand band = mercyGame && placements < _settings.MercyPlacements ? _settings.Mercy : _settings.BandFor(placements);
            bool helpful = placements < _settings.HelpfulUntilPlacement;

            if (_settings.FitGuarantee == FitGuarantee.WholeTray && DealPlaceable(board, band, helpful, tray)) return;

            DealRaw(board, band, helpful, tray);
            if (_settings.FitGuarantee != FitGuarantee.None) EnsureOneFits(board, tray, band);
        }

        /// <summary>The rewarded revive: small pieces that can all be placed on this board.</summary>
        public PieceShape[] DealRescue(Board board)
        {
            var tray = new PieceShape[TraySize];
            DealRescue(board, tray);
            return tray;
        }

        /// <summary>The rewarded revive, dealt into <paramref name="tray"/>, overwriting all three slots. Allocates nothing.</summary>
        public void DealRescue(Board board, PieceShape?[] tray)
        {
            CheckTray(tray);
            if (DealPlaceable(board, _settings.Mercy, helpful: true, tray)) return;

            // The standard catalog's smallest piece is one cell, and a board always has an empty cell
            // after a placement (a full board would have cleared), so three of them always fit.
            for (int slot = 0; slot < TraySize; slot++) tray[slot] = _smallest;
        }

        static void CheckTray(PieceShape?[] tray)
        {
            if (tray == null) throw new ArgumentNullException(nameof(tray));
            if (tray.Length != TraySize) throw new ArgumentException($"The tray must have {TraySize} slots.", nameof(tray));
        }

        /// <summary>Deals up to MaxWholeTrayAttempts trays into <paramref name="tray"/>; true once all of its pieces can be placed.</summary>
        bool DealPlaceable(Board board, DifficultyBand band, bool helpful, PieceShape?[] tray)
        {
            for (int attempt = 0; attempt < _settings.MaxWholeTrayAttempts; attempt++)
            {
                DealRaw(board, band, helpful, tray);
                if (TraySolver.CanPlaceAll(board, tray)) return true;
            }
            return false;
        }

        void DealRaw(Board board, DifficultyBand band, bool helpful, PieceShape?[] tray)
        {
            for (int slot = 0; slot < TraySize; slot++)
            {
                PieceShape? pick = null;
                if (helpful && _rng.NextInt(100) < _settings.HelpfulChancePercent)
                    pick = PickWeighted(band, Filter.CanClearLine, board);
                tray[slot] = pick ?? PickWeighted(band, Filter.None, board) ?? _smallest;
            }
        }

        void EnsureOneFits(Board board, PieceShape?[] tray, DifficultyBand band)
        {
            foreach (var piece in tray)
            {
                if (piece!.FitsAnywhere(board)) return;
            }

            PieceShape? replacement = PickWeighted(band, Filter.FitsAnywhere, board) ?? PickAnyThatFits(board);
            if (replacement != null) tray[_rng.NextInt(TraySize)] = replacement;
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
                var shapes = _byCategory[c];
                for (int i = 0; i < shapes.Length; i++)
                {
                    if (Allows(filter, c, i, board)) allowed.Add(shapes[i]);
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

        bool Allows(Filter filter, int category, int index, Board board)
        {
            switch (filter)
            {
                case Filter.FitsAnywhere: return _byCategory[category][index].FitsAnywhere(board);
                case Filter.CanClearLine: return CanClearLine(category, index, board);
                default: return true;
            }
        }

        bool CanClearLine(int category, int index, Board board)
        {
            if (!_canClearLineKnown || _canClearLineBoard != board)
            {
                for (int c = 0; c < CategoryCount; c++)
                {
                    var shapes = _byCategory[c];
                    for (int i = 0; i < shapes.Length; i++) _canClearLine[c][i] = shapes[i].CanClearLine(board);
                }
                _canClearLineBoard = board;
                _canClearLineKnown = true;
            }
            return _canClearLine[category][index];
        }
    }
}
