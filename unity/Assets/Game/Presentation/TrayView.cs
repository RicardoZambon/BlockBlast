#nullable enable
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The three tray slots. A slot's PieceView is also the piece that gets dragged.</summary>
    public sealed class TrayView : MonoBehaviour
    {
        readonly PieceView[] _slots = new PieceView[PieceGenerator.TraySize];
        BoardLayout _layout;
        bool _built;

        public void Build(BoardLayout layout)
        {
            _layout = layout;
            if (_built) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = new GameObject("Slot " + i);
                slot.transform.SetParent(transform, false);
                _slots[i] = slot.AddComponent<PieceView>();
            }
            _built = true;
        }

        public PieceView Slot(int slot) => _slots[slot];

        /// <summary>Shows each slot's piece in its tray spot at TrayScale and hides empty slots.</summary>
        public void Render(GameSession game)
        {
            for (int slot = 0; slot < _slots.Length; slot++)
            {
                var shape = game.GetTrayPiece(slot);
                if (shape == null)
                {
                    _slots[slot].Hide();
                    continue;
                }
                _slots[slot].Show(shape, _layout.CellSize);
                PutInTray(slot, shape);
            }
        }

        /// <summary>Moves a slot's piece back to its tray spot and size.</summary>
        public void PutInTray(int slot, PieceShape shape)
        {
            var piece = _slots[slot].transform;
            piece.position = _layout.TrayPieceTopLeft(slot, shape.Width, shape.Height);
            piece.localScale = Vector3.one * BoardLayout.TrayScale;
        }
    }
}
