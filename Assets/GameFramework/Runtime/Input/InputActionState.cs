using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// A logical action's fully-sampled state for the current frame. Game code reads this instead
    /// of querying a physical device directly — see <see cref="IInputService"/>.
    /// </summary>
    public readonly struct InputActionState
    {
        public readonly bool IsPressed;
        public readonly bool WasPressedThisFrame;
        public readonly bool WasReleasedThisFrame;
        public readonly float AxisValue;
        public readonly Vector2 Vector2Value;

        public InputActionState(
            bool isPressed, bool wasPressedThisFrame, bool wasReleasedThisFrame,
            float axisValue, Vector2 vector2Value)
        {
            IsPressed = isPressed;
            WasPressedThisFrame = wasPressedThisFrame;
            WasReleasedThisFrame = wasReleasedThisFrame;
            AxisValue = axisValue;
            Vector2Value = vector2Value;
        }

        public static readonly InputActionState None = new InputActionState(false, false, false, 0f, Vector2.zero);
    }
}
