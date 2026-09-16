using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// One pointer's (mouse or a single finger) state for the current frame, in screen-space
    /// pixels (matches <see cref="Camera.ScreenToWorldPoint"/>'s input space) — never mixed with
    /// canvas or world coordinates here; convert explicitly at the call site if needed.
    /// </summary>
    public readonly struct PointerState
    {
        /// <summary>-1 for the mouse pointer; a Unity touch finger id otherwise.</summary>
        public readonly int PointerId;
        public readonly Vector2 ScreenPosition;
        public readonly bool IsDown;
        public readonly bool WasPressedThisFrame;
        public readonly bool WasReleasedThisFrame;

        /// <summary>True if this pointer's position is currently over a UI element that would
        /// receive the raycast (per <see cref="UnityEngine.EventSystems.EventSystem"/>).</summary>
        public readonly bool IsOverUI;

        public PointerState(
            int pointerId, Vector2 screenPosition, bool isDown,
            bool wasPressedThisFrame, bool wasReleasedThisFrame, bool isOverUI)
        {
            PointerId = pointerId;
            ScreenPosition = screenPosition;
            IsDown = isDown;
            WasPressedThisFrame = wasPressedThisFrame;
            WasReleasedThisFrame = wasReleasedThisFrame;
            IsOverUI = isOverUI;
        }

        public static readonly PointerState None = new PointerState(-1, Vector2.zero, false, false, false, false);
    }
}
