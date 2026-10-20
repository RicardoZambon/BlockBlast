#nullable enable
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>One piece as plain squares, with its top-left corner at this transform's position.</summary>
    public sealed class PieceView : MonoBehaviour
    {
        /// <summary>The most cells any shape has (the 3x3 square).</summary>
        public const int MaxCells = 9;

        /// <summary>Pieces draw above the board.</summary>
        public const int SortingOrder = 10;

        readonly SpriteRenderer[] _squares = new SpriteRenderer[MaxCells];
        bool _built;

        public PieceShape? Shape { get; private set; }
        public int VisibleCells { get; private set; }

        /// <summary>Lays out the shape's cells at cellSize local units each, reusing the same nine squares.</summary>
        public void Show(PieceShape shape, float cellSize)
        {
            EnsureBuilt();
            Shape = shape;
            int used = 0;
            for (int row = 0; row < shape.Height; row++)
            {
                for (int col = 0; col < shape.Width; col++)
                {
                    if ((shape.BaseMask & (1UL << Board.BitIndex(row, col))) == 0) continue;
                    var square = _squares[used++];
                    square.gameObject.SetActive(true);
                    square.transform.localPosition = new Vector3((col + 0.5f) * cellSize, -(row + 0.5f) * cellSize, 0);
                    square.transform.localScale = Vector3.one * (cellSize * CellSprite.Fill);
                }
            }
            VisibleCells = used;
            for (int i = used; i < MaxCells; i++) _squares[i].gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Shape = null;
            VisibleCells = 0;
            gameObject.SetActive(false);
        }

        void EnsureBuilt()
        {
            if (_built) return;
            for (int i = 0; i < MaxCells; i++)
            {
                var square = new GameObject("Square " + i);
                square.transform.SetParent(transform, false);
                var renderer = square.AddComponent<SpriteRenderer>();
                renderer.sprite = CellSprite.Square;
                renderer.color = Palette.Filled;
                renderer.sortingOrder = SortingOrder;
                _squares[i] = renderer;
            }
            _built = true;
        }
    }
}
