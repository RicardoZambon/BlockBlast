#nullable enable
using BlockPuzzle.Core;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// One drag from the tray: which slot is held, where its piece would land, and the cells it would fill and clear.
    /// Plain C#, so the drag rules are tested without Unity objects.
    /// </summary>
    public sealed class DragSession
    {
        public bool IsDragging => Slot >= 0;

        /// <summary>The tray slot being dragged, or -1.</summary>
        public int Slot { get; private set; } = -1;

        public bool HasTarget { get; private set; }
        public int TargetRow { get; private set; } = -1;
        public int TargetCol { get; private set; } = -1;

        /// <summary>The cells the piece would fill; 0 without a target.</summary>
        public ulong TargetMask { get; private set; }

        /// <summary>The cells that would clear; 0 without a target.</summary>
        public ulong ClearMask { get; private set; }

        int _guideRow = -1;
        int _guideCol = -1;

        /// <summary>Lets pieces land only with their top-left on (row, col), as in the tutorial; (-1, -1) lets them land anywhere.</summary>
        public void SetGuide(int row, int col)
        {
            _guideRow = row;
            _guideCol = col;
        }

        /// <summary>Starts dragging a slot's piece. False while another drag runs, for an empty or missing slot, or after game over.</summary>
        public bool TryBegin(GameSession game, int slot)
        {
            if (IsDragging || game.IsGameOver || (uint)slot >= PieceGenerator.TraySize || game.GetTrayPiece(slot) == null) return false;
            Slot = slot;
            ClearTarget();
            return true;
        }

        /// <summary>Moves the piece's top-left corner to (topLeftRow, topLeftCol), in cells from the board's top-left corner. Allocates nothing.</summary>
        public void Move(GameSession game, float topLeftRow, float topLeftCol)
        {
            if (!IsDragging) return;
            var shape = game.GetTrayPiece(Slot);
            if (shape != null && SnapFinder.TryFind(game, Slot, topLeftRow, topLeftCol, out int row, out int col, _guideRow, _guideCol) &&
                shape.TryGetMask(row, col, out ulong mask))
            {
                HasTarget = true;
                TargetRow = row;
                TargetCol = col;
                TargetMask = mask;
                ClearMask = game.Board.Place(mask).ClearedMask;
            }
            else
            {
                ClearTarget();
            }
        }

        /// <summary>Places the piece at the target and ends the drag. Without a target it only ends the drag and returns false.</summary>
        public bool TryDrop(GameSession game, out PlacementOutcome outcome)
        {
            outcome = default;
            bool placed = IsDragging && HasTarget && game.TryPlace(Slot, TargetRow, TargetCol, out outcome);
            Cancel();
            return placed;
        }

        /// <summary>Ends the drag without placing.</summary>
        public void Cancel()
        {
            Slot = -1;
            ClearTarget();
        }

        void ClearTarget()
        {
            HasTarget = false;
            TargetRow = -1;
            TargetCol = -1;
            TargetMask = 0;
            ClearMask = 0;
        }
    }
}
