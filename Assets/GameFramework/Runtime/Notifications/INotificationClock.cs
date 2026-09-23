using System;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Time source for scheduling/tests - see CLAUDE.md's Phase 18 brief, section 33 ("do not
    /// scatter DateTime.UtcNow; reuse an existing clock abstraction, or create the smallest necessary
    /// one"). Deliberately a small, independent duplicate of the shape
    /// <c>RemoteConfig.LiveOps.ILiveOpsClock</c> already established, rather than a shared type - the
    /// same reasoning that keeps a fake <c>ITimeService</c> duplicated per test assembly: this
    /// interface is one property, and referencing the entire <c>GameFramework.RemoteConfig</c>
    /// assembly just to reuse it would work against this assembly's own minimal-sibling independence.
    /// </summary>
    public interface INotificationClock
    {
        DateTime UtcNow { get; }
    }
}
