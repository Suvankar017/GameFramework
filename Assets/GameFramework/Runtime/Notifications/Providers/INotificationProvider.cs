using System;
using System.Collections.Generic;

namespace GameFramework.Notifications.Providers
{
    /// <summary>
    /// The notification provider boundary - see CLAUDE.md's Phase 18 brief, sections 12/39. No Unity
    /// Mobile Notifications/Firebase/OneSignal adapter exists in this project (none of those
    /// packages/SDKs is installed - see <c>Packages/manifest.json</c>); a future adapter lives in its
    /// own assembly (e.g. <c>GameFramework.Notifications.UnityMobileNotifications</c>), referencing
    /// only that installed package, implementing this interface against its real, installed API.
    /// <see cref="NotificationService"/> owns all game-facing scheduling/content/localization logic; a
    /// provider is only ever asked to initialize, report permission, schedule, and cancel.
    ///
    /// Scheduling/cancellation callbacks must complete on the Unity main thread (CLAUDE.md's Phase 18
    /// brief, section 32) - a provider backed by a native SDK that calls back from another thread is
    /// responsible for marshaling onto the main thread itself before invoking any callback here.
    /// </summary>
    public interface INotificationProvider
    {
        void Initialize(Action<bool> onComplete);

        bool IsSupported { get; }

        NotificationPermissionStatus GetPermissionStatus();

        bool CanRequestPermission();

        /// <summary>Never called automatically by this framework (section 13) - only ever in
        /// response to an explicit <see cref="INotificationService.RequestPermission"/> call.</summary>
        void RequestPermission(Action<NotificationPermissionStatus> onResult);

        void RegisterChannel(NotificationChannelDefinition channel);

        /// <summary><paramref name="request"/> is already fully resolved (concrete text, UTC time) -
        /// a provider never sees a <see cref="NotificationText"/> localization key.</summary>
        NotificationResult Schedule(NotificationRequest request);

        NotificationResult Cancel(NotificationId id);

        void CancelAll();

        IReadOnlyList<NotificationRequest> GetScheduled();

        /// <summary>Checked once during <see cref="INotificationService"/> initialization - reports
        /// whether the application was cold-started by tapping a notification. Returns false for a
        /// warm/hot start, or when no such information is available.</summary>
        bool TryGetLaunchNotification(out NotificationOpenedInfo info);

        /// <summary>Fired when a scheduled notification is opened (tapped) by the user.</summary>
        event Action<NotificationOpenedInfo> NotificationOpened;

        /// <summary>Fired when a notification is delivered while the app is already running.</summary>
        event Action<NotificationOpenedInfo> NotificationReceived;
    }
}
