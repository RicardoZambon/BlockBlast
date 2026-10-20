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
