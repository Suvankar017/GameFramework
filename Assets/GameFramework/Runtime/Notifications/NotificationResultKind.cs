namespace GameFramework.Notifications
{
    /// <summary>See CLAUDE.md's Phase 18 brief, section 45 - a structured result instead of throwing
    /// for an expected platform/provider limitation.</summary>
    public enum NotificationResultKind
    {
        Success,
        Unsupported,
        PermissionDenied,
        InvalidRequest,
        ProviderUnavailable,
        SchedulingFailed,

        /// <summary>Reserved for a provider that cannot replace an already-scheduled id and must
        /// reject a duplicate outright - see <see cref="NotificationId"/>'s remarks. The providers
        /// shipped with this phase (<c>NoOpNotificationProvider</c>/<c>MockNotificationProvider</c>)
        /// both support replace and never produce this.</summary>
        AlreadyScheduled,

        Cancelled,
        NotFound
    }
}
