namespace GameFramework.Tutorials.Steps
{
    /// <summary>Which sampled shape of an <see cref="Input.InputActionState"/> completes an
    /// <see cref="InputStep"/>.</summary>
    public enum InputTriggerType
    {
        /// <summary><see cref="GameFramework.Input.InputActionState.WasPressedThisFrame"/>.</summary>
        Pressed,

        /// <summary><see cref="GameFramework.Input.InputActionState.IsPressed"/>, checked every
        /// tick - completes the first tick the action is found held down while this step is active
        /// (does not require a sustained hold duration).</summary>
        Held,

        /// <summary><see cref="GameFramework.Input.InputActionState.WasReleasedThisFrame"/>.</summary>
        Released
    }
}
