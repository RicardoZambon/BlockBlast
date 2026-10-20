#nullable enable
using System;
using System.Collections;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game.Tests
{
    /// <summary>Drags through a GameController, with pointer positions taken from its layout.</summary>
    public static class TestDrags
    {
        /// <summary>The pointer position whose dragged piece has its top-left corner exactly on (row, col).</summary>
        public static Vector2 PointerFor(BoardLayout layout, PieceShape shape, int row, int col) =>
            layout.PointerFor(row, col, shape.Width, shape.Height);

        /// <summary>Presses the piece in a tray slot, drags it to a pointer position and lets go, a frame per step.</summary>
        public static IEnumerator DragAndRelease(GameController game, int slot, Vector2 to)
        {
            if (!game.Drag.Press(game.Layout.TraySlotCenter(slot))) throw new InvalidOperationException($"Pressing tray slot {slot} picked up nothing.");
            yield return null;
            game.Drag.Drag(to);
            yield return null;
            game.Drag.Release(to);
            yield return null;
        }
    }
}
