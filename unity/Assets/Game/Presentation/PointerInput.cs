#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Feeds the primary pointer (mouse, or the first finger on a touchscreen) to the DragController, so other
    /// fingers are ignored. A touch the system takes away, a pause or a loss of focus sends a dragged piece back.
    /// </summary>
    public sealed class PointerInput : MonoBehaviour
    {
        Camera? _camera;
        DragController? _drag;

        public void Init(Camera camera, DragController drag)
        {
            _camera = camera;
            _drag = drag;
        }

        void Update()
        {
            if (_camera == null || _drag == null) return;
            var pointer = Pointer.current;
            if (pointer == null) return;

            // Android cancels a touch when the notification shade, a back gesture or a dialog takes it; that ends as a
            // release, but the player didn't drop the piece.
            bool cancelled = pointer is Touchscreen touchscreen &&
                             touchscreen.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
            Vector2 world = _camera.ScreenToWorldPoint(pointer.position.ReadValue());
            Apply(pointer.press.wasPressedThisFrame, pointer.press.isPressed, pointer.press.wasReleasedThisFrame, cancelled, world);
        }

        /// <summary>
        /// One frame of the primary pointer, in world units. A cancelled release sends the piece back instead of dropping it.
        /// Does nothing while the component is turned off, which is how the game pauses input.
        /// </summary>
        public void Apply(bool pressedThisFrame, bool held, bool releasedThisFrame, bool cancelled, Vector2 world)
        {
            if (_drag == null || !enabled) return;
            if (pressedThisFrame) _drag.Press(world);
            else if (held) _drag.Drag(world);
            if (!releasedThisFrame) return;
            if (cancelled) _drag.Cancel();
            else _drag.Release(world);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _drag != null) _drag.Cancel();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && _drag != null) _drag.Cancel();
        }
    }
}
