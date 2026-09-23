using System;

namespace GameFramework.Notifications
{
    /// <summary>
    /// A provider-independent scheduling request - see CLAUDE.md's Phase 18 brief, section 7. UTC
    /// internally throughout (section 34) - never device-local time.
    /// </summary>
    public sealed class NotificationRequest
    {
        public NotificationId Id { get; }
        public NotificationContent Content { get; }
        public DateTime ScheduledTimeUtc { get; }
        public NotificationRepeatMode Repeat { get; }
        public NotificationPayload Payload { get; }
        public NotificationPriority Priority { get; }
        public string ChannelId { get; }
        public bool PlaySound { get; }

        /// <summary>Null means "leave the badge count untouched" - a provider without badge support
        /// (Android, the shipped NoOp/Mock providers) ignores this either way.</summary>
        public int? BadgeCount { get; }

        public NotificationRequest(
            NotificationId id,
            NotificationContent content,
            DateTime scheduledTimeUtc,
            NotificationRepeatMode repeat = NotificationRepeatMode.None,
            NotificationPayload payload = null,
            NotificationPriority priority = NotificationPriority.Default,
            string channelId = null,
            bool playSound = true,
            int? badgeCount = null)
        {
            Id = id;
            Content = content;
            ScheduledTimeUtc = scheduledTimeUtc.Kind == DateTimeKind.Utc ? scheduledTimeUtc : scheduledTimeUtc.ToUniversalTime();
            Repeat = repeat;
            Payload = payload ?? NotificationPayload.Empty;
            Priority = priority;
            ChannelId = channelId ?? string.Empty;
            PlaySound = playSound;
            BadgeCount = badgeCount;
        }
    }
}
