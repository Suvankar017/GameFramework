using System.Collections.Generic;

namespace GameFramework.Notifications
{
    /// <summary>Development-time snapshot of service health - see CLAUDE.md's Phase 18 brief, section
    /// 30/48. Read-only; never used to drive gameplay logic.</summary>
    public readonly struct NotificationDiagnostics
    {
        public readonly bool IsSupported;
        public readonly NotificationPermissionStatus PermissionStatus;
        public readonly int ScheduledCount;
        public readonly IReadOnlyList<NotificationId> ScheduledIds;
        public readonly string LastError;

        public NotificationDiagnostics(bool isSupported, NotificationPermissionStatus permissionStatus, IReadOnlyList<NotificationId> scheduledIds, string lastError)
        {
            IsSupported = isSupported;
            PermissionStatus = permissionStatus;
            ScheduledIds = scheduledIds;
            ScheduledCount = scheduledIds.Count;
            LastError = lastError;
        }
    }
}
