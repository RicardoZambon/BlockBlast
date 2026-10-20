#nullable enable
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// The 8x8 board as plain squares: filled, empty, the ghost of a dragged piece and the lines it would clear. It also
    /// plays the landing squash and the clearing sweep, without allocating.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        const int CellCount = Board.Size * Board.Size;
        const float SquashSeconds = 0.14f;
        const float SquashDepth = 0.18f;
        const float SweepSeconds = 0.3f;
        const float SweepStepSeconds = 0.025f;   // the delay per cell along the sweep, diagonally from the top left

        SpriteRenderer[]? _cells;           // by bit index, row * 8 + col
        readonly Color[] _shown = new Color[CellCount];
        Board _board;
        float _cellScale;

        ulong _squashMask;
        float _squashTime;
        ulong _sweepMask;
        float _sweepTime;
        bool _sweepDelays;

        public bool IsSquashing => _squashMask != 0;

        public bool IsSweeping => _sweepMask != 0;

        /// <summary>Squeezes these cells and springs them back: the piece landing.</summary>
        public void Squash(ulong mask)
        {
            EndSquash();
            _squashMask = mask;
            _squashTime = 0f;
        }

        /// <summary>
        /// Fades these cleared cells out from the clearing colour, along a diagonal when delays is true and all at once
        /// for reduced motion. A new drag's ghost ends it at once.
        /// </summary>
        public void Sweep(ulong clearedMask, bool delays)
        {
            EndSweep();
            _sweepMask = clearedMask;
            _sweepTime = 0f;
            _sweepDelays = delays;
            if (_cells == null) return;
            for (int i = 0; i < CellCount; i++)
            {
                if ((clearedMask & (1UL << i)) != 0) _cells[i].color = Palette.Clearing;
            }
        }

        void Update()
        {
            if (_cells == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_squashMask != 0)
            {
                _squashTime += dt;
                float t = _squashTime / SquashSeconds;
                if (t >= 1f) EndSquash();
                else ScaleCells(_squashMask, _cellScale * (1f - SquashDepth * Mathf.Sin(t * Mathf.PI)));
            }
            if (_sweepMask != 0)
            {
                _sweepTime += dt;
                bool done = true;
                for (int i = 0; i < CellCount; i++)
                {
                    if ((_sweepMask & (1UL << i)) == 0) continue;
                    float delay = _sweepDelays ? SweepStepSeconds * (i / Board.Size + i % Board.Size) : 0f;
                    float t = Mathf.Clamp01((_sweepTime - delay) / SweepSeconds);
                    if (t < 1f) done = false;
                    _cells[i].color = Color.Lerp(Palette.Clearing, BoardColor(i), t);
                }
                if (done) EndSweep();
            }
        }

        void EndSquash()
        {
            if (_squashMask != 0) ScaleCells(_squashMask, _cellScale);
            _squashMask = 0;
        }

        /// <summary>Puts the swept cells back on the board's own colours.</summary>
        void EndSweep()
        {
            if (_cells != null && _sweepMask != 0)
            {
                for (int i = 0; i < CellCount; i++)
                {
                    if ((_sweepMask & (1UL << i)) == 0) continue;
                    _shown[i] = BoardColor(i);
                    _cells[i].color = _shown[i];
                }
            }
            _sweepMask = 0;
        }

        Color BoardColor(int i) => (_board.Cells & (1UL << i)) != 0 ? Palette.Filled : Palette.Empty;

        void ScaleCells(ulong mask, float scale)
        {
            if (_cells == null) return;
            for (int i = 0; i < CellCount; i++)
            {
                if ((mask & (1UL << i)) != 0) _cells[i].transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

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

            _cellScale = layout.CellSize * CellSprite.Fill;
            for (int row = 0; row < Board.Size; row++)
            {
                for (int col = 0; col < Board.Size; col++)
                {
                    var square = _cells[Board.BitIndex(row, col)].transform;
                    square.position = layout.CellCenter(row, col);
                    square.localScale = new Vector3(_cellScale, _cellScale, 1f);
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
            _board = board;
            if ((ghostMask | clearMask) != 0) EndSweep();   // a new drag: its ghost wins over the last clear's fade
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
