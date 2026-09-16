using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GameFramework.Input
{
    /// <summary>
    /// The one seam between <see cref="InputService"/>'s action-resolution logic and physical
    /// input APIs — both the legacy Input Manager and the New Input System.
    /// <see cref="UnityInputSampler"/> is the real implementation; tests inject a fake so
    /// button/axis/touch/pointer-over-UI behavior is fully deterministic without a live device.
    /// A member reads as "no input" (false/0/<see cref="Vector2.zero"/>) whenever its backend isn't
    /// active for the project's Active Input Handling setting or no matching device is attached —
    /// never throws.
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

        /// <summary>New Input System keyboard key state, via <c>Keyboard.current</c>.</summary>
        bool GetKey(Key key);

        /// <summary>New Input System mouse button state, via <c>Mouse.current</c>.</summary>
        bool GetMouseButton(NewInputMouseButton button);

        /// <summary>Gamepad button state, via <c>Gamepad.current</c>. False if no gamepad is attached.</summary>
        bool GetGamepadButton(GamepadButton button);

        /// <summary>Gamepad trigger value in [0, 1]. 0 if no gamepad is attached.</summary>
        float GetGamepadTrigger(bool rightTrigger);

        /// <summary>Gamepad stick/D-pad value. <see cref="Vector2.zero"/> if no gamepad is attached
        /// or <paramref name="source"/> is <see cref="GamepadStickSource.None"/>.</summary>
        Vector2 GetGamepadStick(GamepadStickSource source);
    }
}
