namespace GameFramework.Input
{
    /// <summary>Which gamepad control an <see cref="InputActionType.Axis"/> binding reads from, in
    /// addition to <see cref="InputActionBindingDefinition.LegacyAxisName"/> — see
    /// <see cref="InputActionBindingDefinition.GamepadAxis"/>.</summary>
    public enum GamepadAxisSource
    {
        /// <summary>No gamepad control bound; the axis is driven by <c>LegacyAxisName</c> only.</summary>
        None,
        LeftTrigger,
        RightTrigger
    }
}
