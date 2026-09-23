using System;
using System.Collections.Generic;

namespace GameFramework.Notifications.Providers
{
    /// <summary>
    /// The default provider when no notification SDK is installed - see CLAUDE.md's Phase 18 brief,
    /// section 39. Reports <see cref="NotificationPermissionStatus.Unsupported"/> and fails every
    /// scheduling attempt with <see cref="NotificationResultKind.Unsupported"/>, so
    /// <see cref="NotificationService"/> stays fully safe to call on a platform/build with no
    /// notification support (Editor without the Mock toggle, standalone, WebGL, an unsupported
    /// console) without crashing or silently pretending to schedule anything.
    /// </summary>
    public sealed class NoOpNotificationProvider : INotificationProvider
    {
        public bool IsSupported => false;

        public event Action<NotificationOpenedInfo> NotificationOpened { add { } remove { } }
        public event Action<NotificationOpenedInfo> NotificationReceived { add { } remove { } }

        public void Initialize(Action<bool> onComplete) => onComplete?.Invoke(true);

        public NotificationPermissionStatus GetPermissionStatus() => NotificationPermissionStatus.Unsupported;

        public bool CanRequestPermission() => false;

        public void RequestPermission(Action<NotificationPermissionStatus> onResult) =>
            onResult?.Invoke(NotificationPermissionStatus.Unsupported);

        public void RegisterChannel(NotificationChannelDefinition channel)
        {
        }

        public NotificationResult Schedule(NotificationRequest request) =>
            NotificationResult.Failure(NotificationResultKind.Unsupported, request.Id, "No notification provider is installed (NoOpNotificationProvider).");

        public NotificationResult Cancel(NotificationId id) => NotificationResult.Failure(NotificationResultKind.Unsupported, id);

        public void CancelAll()
        {
        }

        public IReadOnlyList<NotificationRequest> GetScheduled() => Array.Empty<NotificationRequest>();

        public bool TryGetLaunchNotification(out NotificationOpenedInfo info)
        {
            info = default;
            return false;
        }
    }
}
