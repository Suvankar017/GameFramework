namespace GameFramework.Platform
{
    /// <summary>Coarse platform identity. Prefer <see cref="IDeviceInfoService.Supports"/> for
    /// feature checks - a platform is not a reliable proxy for a capability (see CLAUDE.md's
    /// Phase 14 brief, sections 5-6). This exists for the small number of cases that genuinely need
    /// to branch on the platform itself.</summary>
    public enum PlatformType
    {
        Unknown,
        Editor,
        Android,
        IOS,
        Windows,
        MacOS,
        Linux
    }
}
