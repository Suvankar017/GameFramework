using System;
using System.Text;
using GameFramework.DeepLinks;
using GameFramework.Runtime.Events;

namespace GameFramework.Notifications.Integration
{
    /// <summary>
    /// Optional Notifications -&gt; DeepLinks bridge - see CLAUDE.md's Phase 18 brief, section 26. A
    /// plain <see cref="IDisposable"/> a game constructs itself, never registered by
    /// <see cref="NotificationsBootstrapper"/>/<see cref="DeepLinksBootstrapper"/> - the same "opt-in,
    /// not bootstrapper-registered" pattern <c>Monetization.Integration.AdPlacementRewardBridge"</c>/
    /// <c>Analytics.Integration</c>'s bridges already establish. <see cref="NotificationService"/>
    /// never hard-codes a destination (e.g. "DailyRewardScreen") - it only transports
    /// <see cref="NotificationPayload.Route"/>/<see cref="NotificationPayload.Parameters"/>; this
    /// bridge is what turns that intent into an actual <see cref="IDeepLinkService.Process"/> call,
    /// exactly the same chain a real inbound deep link goes through (so the same registered
    /// <see cref="IDeepLinkHandler"/>s - e.g. <see cref="NavigationDeepLinkHandler"/> - handle both).
    /// </summary>
    public sealed class NotificationDeepLinkBridge : IDisposable
    {
        private readonly IEventService _events;
        private readonly IDeepLinkService _deepLinks;
        private readonly string _scheme;

        public NotificationDeepLinkBridge(IEventService events, IDeepLinkService deepLinks, string scheme = "notification")
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _deepLinks = deepLinks ?? throw new ArgumentNullException(nameof(deepLinks));
            _scheme = string.IsNullOrEmpty(scheme) ? "notification" : scheme;

            _events.Subscribe<NotificationOpenedEvent>(OnNotificationOpened);
        }

        public void Dispose() => _events.Unsubscribe<NotificationOpenedEvent>(OnNotificationOpened);

        private void OnNotificationOpened(NotificationOpenedEvent evt)
        {
            NotificationPayload payload = evt.Info.Payload;
            if (string.IsNullOrEmpty(payload.Route))
            {
                return; // A notification with no route is valid (e.g. "just open the app") - nothing to route.
            }

            _deepLinks.Process(BuildRawUri(payload));
        }

        private string BuildRawUri(NotificationPayload payload)
        {
            var builder = new StringBuilder(_scheme).Append("://").Append(payload.Route);

            bool first = true;
            foreach (var parameter in payload.Parameters)
            {
                builder.Append(first ? '?' : '&');
                first = false;
                builder.Append(Uri.EscapeDataString(parameter.Key)).Append('=').Append(Uri.EscapeDataString(parameter.Value ?? string.Empty));
            }

            return builder.ToString();
        }
    }
}
