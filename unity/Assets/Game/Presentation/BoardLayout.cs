#nullable enable
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Where everything goes, in world units. From top to bottom: a band for the score, the 8x8 board, a gap and
    /// the three-slot tray, stacked and centred in the safe area.
    /// </summary>
    public readonly struct BoardLayout
    {
        /// <summary>Tray pieces are drawn at this fraction of full size.</summary>
        public const float TrayScale = 0.5f;

        /// <summary>A dragged piece's bottom edge floats this many cells above the pointer.</summary>
        public const float LiftCells = 1.5f;

        const int BoardCells = Board.Size;
        const int TraySlots = PieceGenerator.TraySize;
        const float SlotCells = (float)BoardCells / TraySlots;
        const float ScoreCells = 1.5f;
        const float GapCells = 0.75f;
        const float TrayCells = 3f;
        const float StackCells = ScoreCells + BoardCells + GapCells + TrayCells;
        const float WidthShare = 0.92f;

        readonly float _trayTop;

        /// <param name="safeWorldRect">The visible area in world units, minus notches and other cut-outs.</param>
        /// <param name="bannerHeight">World units at the top of the safe area kept free for the banner ad.</param>
        public BoardLayout(Rect safeWorldRect, float bannerHeight = 0f)
        {
            float usableHeight = Mathf.Max(0f, safeWorldRect.height - bannerHeight);
            CellSize = Mathf.Min(safeWorldRect.width * WidthShare / BoardCells, usableHeight / StackCells);
            float stackTop = safeWorldRect.yMin + usableHeight / 2 + StackCells * CellSize / 2;
            BoardTopLeft = new Vector2(safeWorldRect.center.x - BoardCells * CellSize / 2, stackTop - ScoreCells * CellSize);
            _trayTop = BoardTopLeft.y - (BoardCells + GapCells) * CellSize;
        }

        public float CellSize { get; }

        /// <summary>The world position of the board's top-left corner.</summary>
        public Vector2 BoardTopLeft { get; }

        /// <summary>The world y of the top of the score band above the board, where the HUD goes.</summary>
        public float ScoreBandTop => BoardTopLeft.y + ScoreCells * CellSize;

        /// <summary>The centre of a board cell; row 0 is the top row.</summary>
        public Vector2 CellCenter(int row, int col) =>
            BoardTopLeft + new Vector2((col + 0.5f) * CellSize, -(row + 0.5f) * CellSize);

        /// <summary>A world point as cells from the board's top-left corner: rows down, columns right.</summary>
        public void ToGrid(Vector2 world, out float row, out float col)
        {
            col = (world.x - BoardTopLeft.x) / CellSize;
            row = (BoardTopLeft.y - world.y) / CellSize;
        }

        public Vector2 TraySlotCenter(int slot) =>
            new Vector2(BoardTopLeft.x + (slot + 0.5f) * SlotCells * CellSize, _trayTop - TrayCells / 2 * CellSize);

        /// <summary>The tray slot under a world point, or -1. Each slot is a third of the tray band.</summary>
        public int TraySlotAt(Vector2 world)
        {
            float x = (world.x - BoardTopLeft.x) / CellSize;
            float y = (_trayTop - world.y) / CellSize;
            if (x < 0 || x >= BoardCells || y < 0 || y > TrayCells) return -1;
            return Mathf.Min((int)(x / SlotCells), TraySlots - 1);
        }

        /// <summary>Where a tray piece's top-left corner goes so that, drawn at TrayScale, it is centred in its slot.</summary>
        public Vector2 TrayPieceTopLeft(int slot, int width, int height)
        {
            float scaledCell = CellSize * TrayScale;
            return TraySlotCenter(slot) + new Vector2(-width * scaledCell / 2, height * scaledCell / 2);
        }

        /// <summary>Where the pointer goes so a dragged piece's top-left lands on (row, col): DraggedTopLeft turned around.</summary>
        public Vector2 PointerFor(int row, int col, int width, int height) =>
            BoardTopLeft + new Vector2((col + width / 2f) * CellSize, -(row + LiftCells + height) * CellSize);

        /// <summary>Where a dragged piece's top-left corner goes: centred on the pointer, its bottom edge LiftCells above it.</summary>
        public Vector2 DraggedTopLeft(Vector2 pointerWorld, int width, int height) =>
            pointerWorld + new Vector2(-width * CellSize / 2, (LiftCells + height) * CellSize);
    }
}
