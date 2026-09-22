namespace GameFramework.Platform
{
    /// <summary>
    /// Deliberately limited to the two permissions Unity itself exposes a genuine cross-platform
    /// (Android/iOS/Editor) check/request API for -
    /// <see cref="UnityEngine.Application.HasUserAuthorization"/>/
    /// <see cref="UnityEngine.Application.RequestUserAuthorization"/>. Anything else (location,
    /// notifications, storage) needs a native plugin per platform, which is out of scope for this
    /// phase (see CLAUDE.md's Phase 14 brief, section 34) - a game with that need implements its own
    /// <see cref="IPermissionService"/> the same way <c>Feedback.MobileHapticProvider</c> documents
    /// as the extension seam for custom haptics.
    /// </summary>
    public enum PlatformPermission
    {
        Camera,
        Microphone
    }
}
