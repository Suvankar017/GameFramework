namespace GameFramework.Cameras
{
    /// <summary>Which built-in <see cref="ICameraMode"/> a <see cref="CameraConfiguration"/>
    /// authors - a small, fixed set (CLAUDE.md's Phase 11 brief, section 8: "avoid creating dozens
    /// of modes"). A game can still assign a fully custom <see cref="ICameraMode"/> at runtime via
    /// <see cref="CameraController.SetMode"/> without this enum needing a new member.</summary>
    public enum CameraModeKind
    {
        Follow,
        Static,
        TargetLook,
        Manual
    }
}
