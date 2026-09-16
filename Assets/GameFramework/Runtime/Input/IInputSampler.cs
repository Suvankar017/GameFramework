using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// The one seam between <see cref="InputService"/>'s action-resolution logic and physical
    /// input APIs. <see cref="UnityInputSampler"/> is the real implementation; tests inject a fake
    /// so button/axis/touch/pointer-over-UI behavior is fully deterministic without a live device.
    /// </summary>
    public interface IInputSampler
    {
        bool GetKey(KeyCode key);
        bool GetMouseButton(int button);
        Vector2 MousePosition { get; }

        /// <summary>Raw (unsmoothed) value of a legacy Input Manager axis, or 0 if the axis is not
        /// configured in Project Settings &gt; Input Manager.</summary>
        float GetAxisRaw(string axisName);

        int TouchCount { get; }
        RawTouch GetTouch(int index);

        /// <summary>True if a UI element would receive a raycast at this pointer's current
        /// position. <paramref name="pointerId"/> is -1 for the mouse pointer.</summary>
        bool IsPointerOverUI(int pointerId);
    }
}
