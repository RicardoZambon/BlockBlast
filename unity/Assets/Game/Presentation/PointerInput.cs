#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockPuzzle.Game
{
    /// <summary>
    /// Feeds the primary pointer (mouse, or the first finger on a touchscreen) to the DragController, so other
    /// fingers are ignored. Pausing the app sends a dragged piece back.
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

            Vector2 world = _camera.ScreenToWorldPoint(pointer.position.ReadValue());
            if (pointer.press.wasPressedThisFrame) _drag.Press(world);
            else if (pointer.press.isPressed) _drag.Drag(world);
            if (pointer.press.wasReleasedThisFrame) _drag.Release(world);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _drag != null) _drag.Cancel();
        }
    }
}
