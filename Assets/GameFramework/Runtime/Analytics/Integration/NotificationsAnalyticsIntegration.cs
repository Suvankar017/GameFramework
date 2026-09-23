using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.DeepLinks;
using GameFramework.Notifications;
using GameFramework.Runtime.Events;

namespace GameFramework.Analytics.Integration
{
    /// <summary>
    /// Optional Notifications/DeepLinks -&gt; Analytics bridge - see CLAUDE.md's Phase 18 brief, section
    /// 29. Consumes Phase 18's own published events only; opt-in, not bootstrapper-registered - see
    /// <see cref="GameFlowAnalyticsIntegration"/>'s remarks for the pattern. Depends only on the event
    /// types it subscribes to, so it works whether or not <c>Notifications.INotificationService</c>/
    /// <c>DeepLinks.IDeepLinkService</c> end up registered in a given game.
    /// </summary>
    public sealed class NotificationsAnalyticsIntegration : IDisposable
    {
        private readonly IEventService _events;
        private readonly IAnalyticsService _analytics;
        private readonly IDiagnosticsService _diagnostics;

        public NotificationsAnalyticsIntegration(IEventService events, IAnalyticsService analytics, IDiagnosticsService diagnostics = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _diagnostics = diagnostics;

            _events.Subscribe<NotificationScheduledEvent>(OnNotificationScheduled);
            _events.Subscribe<NotificationCancelledEvent>(OnNotificationCancelled);
            _events.Subscribe<NotificationOpenedEvent>(OnNotificationOpened);
            _events.Subscribe<NotificationReceivedEvent>(OnNotificationReceived);
            _events.Subscribe<NotificationPermissionRequestedEvent>(OnNotificationPermissionRequested);
            _events.Subscribe<NotificationPermissionChangedEvent>(OnNotificationPermissionChanged);
            _events.Subscribe<DeepLinkReceivedEvent>(OnDeepLinkReceived);
            _events.Subscribe<DeepLinkHandledEvent>(OnDeepLinkHandled);
            _events.Subscribe<DeepLinkRejectedEvent>(OnDeepLinkRejected);
        }

        public void Dispose()
        {
            _events.Unsubscribe<NotificationScheduledEvent>(OnNotificationScheduled);
            _events.Unsubscribe<NotificationCancelledEvent>(OnNotificationCancelled);
            _events.Unsubscribe<NotificationOpenedEvent>(OnNotificationOpened);
            _events.Unsubscribe<NotificationReceivedEvent>(OnNotificationReceived);
            _events.Unsubscribe<NotificationPermissionRequestedEvent>(OnNotificationPermissionRequested);
            _events.Unsubscribe<NotificationPermissionChangedEvent>(OnNotificationPermissionChanged);
            _events.Unsubscribe<DeepLinkReceivedEvent>(OnDeepLinkReceived);
            _events.Unsubscribe<DeepLinkHandledEvent>(OnDeepLinkHandled);
            _events.Unsubscribe<DeepLinkRejectedEvent>(OnDeepLinkRejected);
        }

        private void OnNotificationScheduled(NotificationScheduledEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Notifications", $"scheduled:{evt.Id}");
            _analytics.Track(EventNames.NotificationScheduled, new Dictionary<string, object> { ["notification_id"] = evt.Id.Value });
        }

        private void OnNotificationCancelled(NotificationCancelledEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Notifications", $"cancelled:{evt.Id}");
            _analytics.Track(EventNames.NotificationCancelled, new Dictionary<string, object> { ["notification_id"] = evt.Id.Value });
        }

        private void OnNotificationOpened(NotificationOpenedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Notifications", $"opened:{evt.Info.Id}");
            _analytics.Track(EventNames.NotificationOpened, new Dictionary<string, object>
            {
                ["notification_id"] = evt.Info.Id.Value,
                ["route"] = evt.Info.Payload.Route,
                ["simulated"] = evt.Info.WasSimulated
            });
        }

        private void OnNotificationReceived(NotificationReceivedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Notifications", $"received:{evt.Info.Id}");
            _analytics.Track(EventNames.NotificationReceived, new Dictionary<string, object>
            {
                ["notification_id"] = evt.Info.Id.Value,
                ["simulated"] = evt.Info.WasSimulated
            });
        }

        private void OnNotificationPermissionRequested(NotificationPermissionRequestedEvent evt) =>
            _analytics.Track(EventNames.NotificationPermissionRequested);

        private void OnNotificationPermissionChanged(NotificationPermissionChangedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Notifications", $"permission_changed:{evt.Status}");
            _analytics.Track(EventNames.NotificationPermissionChanged, new Dictionary<string, object> { ["status"] = evt.Status.ToString() });
        }

        private void OnDeepLinkReceived(DeepLinkReceivedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("DeepLinks", "received");
            _analytics.Track(EventNames.DeepLinkReceived);
        }

        private void OnDeepLinkHandled(DeepLinkHandledEvent evt)
        {
            _diagnostics?.AddBreadcrumb("DeepLinks", "handled");
            _analytics.Track(EventNames.DeepLinkHandled);
        }

        private void OnDeepLinkRejected(DeepLinkRejectedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("DeepLinks", $"rejected:{evt.Reason}");
            _analytics.Track(EventNames.DeepLinkRejected, new Dictionary<string, object> { ["reason"] = evt.Reason ?? string.Empty });
        }
    }
}
