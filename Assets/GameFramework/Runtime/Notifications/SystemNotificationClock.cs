using System;

namespace GameFramework.Notifications
{
    public sealed class SystemNotificationClock : INotificationClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
