namespace GameFramework.Platform
{
    /// <summary>Deliberately limited to capabilities Unity itself exposes a reliable
    /// non-invasive query for (see CLAUDE.md's Phase 14 brief, section 9). Notably excludes
    /// Camera/Microphone hardware presence - enumerating those devices touches sensitive platform
    /// surface for a passive capability check, which section 80's privacy-minimal design rules out;
    /// <see cref="PlatformPermission"/> covers the permission side of that instead.</summary>
    public enum DeviceCapability
    {
        Haptics,
        Gyroscope,
        Accelerometer,
        Touch,
        MultiTouch,
        LocationService,
        Clipboard,
        Audio
    }
}
