namespace GameFramework.Input
{
    /// <summary>Mouse button as read through the New Input System's <c>Mouse.current</c>, kept as
    /// its own enum (rather than reusing the New Input System's own types) so
    /// <see cref="InputActionBindingDefinition"/> stays simple to author from the Inspector,
    /// mirroring the legacy <see cref="InputActionBindingDefinition.MouseButtons"/> int indices.</summary>
    public enum NewInputMouseButton
    {
        Left,
        Right,
        Middle
    }
}
