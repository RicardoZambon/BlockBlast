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
