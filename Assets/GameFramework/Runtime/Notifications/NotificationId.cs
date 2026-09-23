using System;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Stable, game-supplied notification key (e.g. "daily_reward_reminder") - see CLAUDE.md's Phase
    /// 18 brief, section 9. Deliberately never auto-generated: an uncontrolled random id would make
    /// cancellation/replacement impossible, so the id IS the cancellation/replace key. Scheduling a
    /// request whose id is already scheduled replaces the existing one (see
    /// <see cref="INotificationService.Schedule"/>'s remarks) - the same "identical id replaces"
    /// behavior both Android and iOS already apply natively.
    /// </summary>
    public readonly struct NotificationId : IEquatable<NotificationId>
    {
        public readonly string Value;

        public NotificationId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(NotificationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is NotificationId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(NotificationId left, NotificationId right) => left.Equals(right);
        public static bool operator !=(NotificationId left, NotificationId right) => !left.Equals(right);
    }
}
