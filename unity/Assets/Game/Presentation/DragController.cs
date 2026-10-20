#nullable enable
using System;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Drags a tray piece: it lifts above the pointer and grows to board size, snaps to the nearest legal spot with a
    /// ghost and the lines it would clear, and on release is placed there or eases back to the tray.
    /// </summary>
    public sealed class DragController : MonoBehaviour
    {
        public const float GrowSeconds = 0.08f;
        public const float ReturnSeconds = 0.15f;

        /// <summary>Snapping starts only after the pointer moves this far, so a tap or a press held still never places a piece.</summary>
        public const float DragThresholdCells = 0.25f;

        readonly DragSession _session = new DragSession();
        BoardLayout _layout;
        BoardView? _board;
        TrayView? _tray;
        Func<GameSession>? _game;
        Action<PlacementOutcome>? _placed;

        Vector2 _pressPoint;
        bool _moved;
        float _scale;

        int _returnSlot = -1;
        Vector3 _returnFrom;
        float _returnFromScale;
        float _returnProgress;

        public bool IsDragging => _session.IsDragging;
        public bool IsReturning => _returnSlot >= 0;

        public void Init(BoardLayout layout, BoardView board, TrayView tray, Func<GameSession> game, Action<PlacementOutcome> placed)
        {
            _layout = layout;
            _board = board;
            _tray = tray;
            _game = game;
            _placed = placed;
        }

        /// <summary>Starts a drag when the point is over a tray piece. Ignored while a piece is dragged or returning.</summary>
        public bool Press(Vector2 world)
        {
            if (_game == null || IsDragging || IsReturning) return false;
            int slot = _layout.TraySlotAt(world);
            if (slot < 0 || !_session.TryBegin(_game(), slot)) return false;
            _pressPoint = world;
            _moved = false;
            _scale = BoardLayout.TrayScale;
            MovePiece(world);
            return true;
        }

        /// <summary>Moves the dragged piece and updates the ghost. Allocates nothing.</summary>
        public void Drag(Vector2 world)
        {
            if (_game == null || _board == null || !IsDragging) return;
            MovePiece(world);
            if (!_moved)
            {
                float threshold = DragThresholdCells * _layout.CellSize;
                if ((world - _pressPoint).sqrMagnitude < threshold * threshold) return;
                _moved = true;
            }

            var game = _game();
            var shape = game.GetTrayPiece(_session.Slot);
            if (shape == null) return;
            _layout.ToGrid(_layout.DraggedTopLeft(world, shape.Width, shape.Height), out float row, out float col);
            _session.Move(game, row, col);
            _board.ShowGhost(game.Board, _session.TargetMask, _session.ClearMask);
        }

        /// <summary>Places the piece where the last Drag snapped it, without snapping again; otherwise sends it back.</summary>
        public void Release(Vector2 world)
        {
            if (_game == null || _board == null || !IsDragging) return;
            int slot = _session.Slot;
            var game = _game();
            if (_session.TryDrop(game, out var outcome))
            {
                _board.Render(game.Board);
                _placed?.Invoke(outcome);
                return;
            }
            _board.Render(game.Board);
            StartReturn(slot);
        }

        /// <summary>Sends the dragged piece back without placing it, for a pause or a lost pointer.</summary>
        public void Cancel()
        {
            if (_game == null || _board == null || !IsDragging) return;
            int slot = _session.Slot;
            _session.Cancel();
            _board.Render(_game().Board);
            StartReturn(slot);
        }

        void Update()
        {
            if (_tray == null || _game == null) return;
            float dt = Time.unscaledDeltaTime;
            if (IsDragging)
            {
                _scale = Mathf.MoveTowards(_scale, 1f, dt * (1f - BoardLayout.TrayScale) / GrowSeconds);
                _tray.Slot(_session.Slot).transform.localScale = Vector3.one * _scale;
                return;
            }
            if (!IsReturning) return;

            var shape = _game().GetTrayPiece(_returnSlot);
            if (shape == null)
            {
                _returnSlot = -1;
                return;
            }
            _returnProgress = Mathf.Min(1f, _returnProgress + dt / ReturnSeconds);
            var piece = _tray.Slot(_returnSlot).transform;
            Vector3 home = _layout.TrayPieceTopLeft(_returnSlot, shape.Width, shape.Height);
            piece.position = Vector3.Lerp(_returnFrom, home, _returnProgress);
            piece.localScale = Vector3.one * Mathf.Lerp(_returnFromScale, BoardLayout.TrayScale, _returnProgress);
            if (_returnProgress < 1f) return;
            _tray.PutInTray(_returnSlot, shape);
            _returnSlot = -1;
        }

        void MovePiece(Vector2 world)
        {
            if (_tray == null || _game == null) return;
            var shape = _game().GetTrayPiece(_session.Slot);
            if (shape == null) return;
            _tray.Slot(_session.Slot).transform.position = _layout.DraggedTopLeft(world, shape.Width, shape.Height);
        }

        void StartReturn(int slot)
        {
            if (_tray == null) return;
            var piece = _tray.Slot(slot).transform;
            _returnSlot = slot;
            _returnFrom = piece.position;
            _returnFromScale = piece.localScale.x;
            _returnProgress = 0f;
        }
    }
}
