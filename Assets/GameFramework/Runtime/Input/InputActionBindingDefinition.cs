using System;
using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// One action's binding to physical input sources. Which fields apply depends on
    /// <see cref="Type"/>: <see cref="InputActionType.Button"/> uses
    /// <see cref="KeyboardKeys"/>/<see cref="MouseButtons"/>; <see cref="InputActionType.Axis"/>
    /// uses <see cref="LegacyAxisName"/>; <see cref="InputActionType.Vector2"/> uses
    /// <see cref="LegacyAxisNameX"/>/<see cref="LegacyAxisNameY"/>.
    /// </summary>
    [Serializable]
    public sealed class InputActionBindingDefinition
    {
        public string ActionName;
        public InputActionType Type = InputActionType.Button;

        [Tooltip("Button: any key being held satisfies the action.")]
        public KeyCode[] KeyboardKeys = Array.Empty<KeyCode>();

        [Tooltip("Button: mouse button indices (0 = left, 1 = right, 2 = middle).")]
        public int[] MouseButtons = Array.Empty<int>();

        [Tooltip("Axis: a legacy Input Manager axis name, e.g. \"Horizontal\".")]
        public string LegacyAxisName;

        [Tooltip("Vector2: legacy Input Manager axis names for X and Y.")]
        public string LegacyAxisNameX;
        public string LegacyAxisNameY;
    }
}
