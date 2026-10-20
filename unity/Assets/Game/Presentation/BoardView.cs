#nullable enable
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The 8x8 board as plain squares: filled, empty, the ghost of a dragged piece and the lines it would clear.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        const int CellCount = Board.Size * Board.Size;

        SpriteRenderer[]? _cells;           // by bit index, row * 8 + col
        readonly Color[] _shown = new Color[CellCount];

        /// <summary>Creates the 64 squares the first time, then lays them out for this layout.</summary>
        public void Build(BoardLayout layout)
        {
            if (_cells == null)
            {
                _cells = new SpriteRenderer[CellCount];
                for (int i = 0; i < CellCount; i++)
                {
                    var square = new GameObject("Cell " + i);
                    square.transform.SetParent(transform, false);
                    var renderer = square.AddComponent<SpriteRenderer>();
                    renderer.sprite = CellSprite.Square;
                    renderer.sortingOrder = 0;
                    _cells[i] = renderer;
                }
            }

            for (int row = 0; row < Board.Size; row++)
            {
                for (int col = 0; col < Board.Size; col++)
                {
                    var square = _cells[Board.BitIndex(row, col)].transform;
                    square.position = layout.CellCenter(row, col);
                    square.localScale = Vector3.one * (layout.CellSize * CellSprite.Fill);
                }
            }
            ShowGhost(Board.Empty, 0, 0, force: true);
        }

        public void Render(Board board) => ShowGhost(board, 0, 0);

        /// <summary>
        /// Colours each cell: Clearing for cells in lines that would clear, then Ghost for the dragged piece, then Filled or Empty.
        /// Only cells whose colour changes are touched, and nothing is allocated.
        /// </summary>
        public void ShowGhost(Board board, ulong ghostMask, ulong clearMask) => ShowGhost(board, ghostMask, clearMask, force: false);

        public Color CellColor(int row, int col) => _cells == null ? Color.clear : _cells[Board.BitIndex(row, col)].color;

        void ShowGhost(Board board, ulong ghostMask, ulong clearMask, bool force)
        {
            if (_cells == null) return;
            for (int i = 0; i < CellCount; i++)
            {
                ulong bit = 1UL << i;
                Color color = (clearMask & bit) != 0 ? Palette.Clearing
                    : (ghostMask & bit) != 0 ? Palette.Ghost
                    : (board.Cells & bit) != 0 ? Palette.Filled
                    : Palette.Empty;
                if (!force && _shown[i] == color) continue;
                _shown[i] = color;
                _cells[i].color = color;
            }
        }
    }
}
