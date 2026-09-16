using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GameFramework.Input
{
    /// <summary>
    /// One action's binding to physical input sources, both legacy (Input Manager) and New Input
    /// System. Which fields apply depends on <see cref="Type"/>: <see cref="InputActionType.Button"/>
    /// uses <see cref="KeyboardKeys"/>/<see cref="MouseButtons"/>/<see cref="NewInputKeyboardKeys"/>/
    /// <see cref="NewInputMouseButtons"/>/<see cref="GamepadButtons"/>; <see cref="InputActionType.Axis"/>
    /// uses <see cref="LegacyAxisName"/>/<see cref="GamepadAxis"/>; <see cref="InputActionType.Vector2"/>
    /// uses <see cref="LegacyAxisNameX"/>/<see cref="LegacyAxisNameY"/>/<see cref="GamepadStick"/>.
    /// A game can bind an action to legacy sources, New Input System sources, both, or neither —
    /// every source list below is additive, so removing one never requires touching the others.
    /// </summary>
    [Serializable]
    public sealed class InputActionBindingDefinition
    {
        public string ActionName;
        public InputActionType Type = InputActionType.Button;

        [Header("Legacy Input Manager")]
        [Tooltip("Button: any key being held satisfies the action.")]
        public KeyCode[] KeyboardKeys = Array.Empty<KeyCode>();

        [Tooltip("Button: mouse button indices (0 = left, 1 = right, 2 = middle).")]
        public int[] MouseButtons = Array.Empty<int>();

        [Tooltip("Axis: a legacy Input Manager axis name, e.g. \"Horizontal\".")]
        public string LegacyAxisName;

        [Tooltip("Vector2: legacy Input Manager axis names for X and Y.")]
        public string LegacyAxisNameX;
        public string LegacyAxisNameY;

        [Header("New Input System (optional; merged with the legacy bindings above)")]
        [Tooltip("Button: New Input System keyboard keys, in addition to KeyboardKeys.")]
        public Key[] NewInputKeyboardKeys = Array.Empty<Key>();

        [Tooltip("Button: New Input System mouse buttons, in addition to MouseButtons.")]
        public NewInputMouseButton[] NewInputMouseButtons = Array.Empty<NewInputMouseButton>();

        [Tooltip("Button: gamepad buttons that satisfy the action.")]
        public GamepadButton[] GamepadButtons = Array.Empty<GamepadButton>();

        [Tooltip("Axis: gamepad trigger to read, combined with LegacyAxisName by taking whichever " +
                 "has the larger magnitude this frame.")]
        public GamepadAxisSource GamepadAxis = GamepadAxisSource.None;

        [Tooltip("Vector2: gamepad stick/D-pad to read, combined with the legacy axis pair by " +
                 "taking whichever has the larger magnitude this frame.")]
        public GamepadStickSource GamepadStick = GamepadStickSource.None;
    }
}
