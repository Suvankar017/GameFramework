namespace GameFramework.Input
{
    /// <summary>Shape of the value a bound input action produces.</summary>
    public enum InputActionType
    {
        /// <summary>Discrete pressed/held/released state.</summary>
        Button,

        /// <summary>A single float, typically in [-1, 1] (e.g. a legacy input axis).</summary>
        Axis,

        /// <summary>A 2D value (e.g. a virtual movement stick built from two axes).</summary>
        Vector2
    }
}
