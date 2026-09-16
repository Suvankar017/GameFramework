namespace GameFramework.Input
{
    /// <summary>Which gamepad control an <see cref="InputActionType.Vector2"/> binding reads from,
    /// in addition to <see cref="InputActionBindingDefinition.LegacyAxisNameX"/>/
    /// <see cref="InputActionBindingDefinition.LegacyAxisNameY"/> — see
    /// <see cref="InputActionBindingDefinition.GamepadStick"/>.</summary>
    public enum GamepadStickSource
    {
        /// <summary>No gamepad control bound; the vector is driven by the legacy axis pair only.</summary>
        None,
        LeftStick,
        RightStick,
        DPad
    }
}
