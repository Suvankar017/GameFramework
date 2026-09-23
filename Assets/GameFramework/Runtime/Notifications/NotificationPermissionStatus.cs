namespace GameFramework.Notifications
{
    /// <summary>
    /// See CLAUDE.md's Phase 18 brief, section 13. Unlike <c>Platform.PermissionStatus</c> (Camera/
    /// Microphone, backed by Unity's own <c>Application.HasUserAuthorization</c>), notifications have
    /// no built-in cross-platform Unity API - this status is entirely provider-reported, which is why
    /// it needs the extra <see cref="Provisional"/>/<see cref="Unsupported"/> states real notification
    /// SDKs (iOS provisional authorization, a platform with no notification support at all) actually
    /// distinguish.
    /// </summary>
    public enum NotificationPermissionStatus
    {
        Unknown,
        NotDetermined,
        Denied,
        Authorized,

        /// <summary>iOS-style "quiet" delivery (no prompt shown to the user, notifications delivered
        /// silently to Notification Center) - a provider that cannot represent this reports
        /// <see cref="Authorized"/> or <see cref="Denied"/> instead.</summary>
        Provisional,

        /// <summary>The current platform/provider has no notification support at all - see
        /// CLAUDE.md's Phase 18 brief, section 39.</summary>
        Unsupported
    }
}
