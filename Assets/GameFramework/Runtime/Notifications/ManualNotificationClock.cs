using System;

namespace GameFramework.Notifications
{
    /// <summary>A settable clock for Editor/QA simulation and deterministic tests - see
    /// CLAUDE.md's Phase 18 brief, section 16.</summary>
    public sealed class ManualNotificationClock : INotificationClock
    {
        public DateTime UtcNow { get; set; }

        public ManualNotificationClock(DateTime initialUtc)
        {
            UtcNow = initialUtc;
        }
    }
}
