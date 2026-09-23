using System;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Time source for Live Ops schedule evaluation - see CLAUDE.md's Phase 17 brief, section
    /// 40. Exists so tests can simulate "before/at/during/after" an event without waiting in real
    /// time, and so <see cref="LiveOpsService"/> never scatters <c>DateTime.UtcNow</c> calls
    /// throughout its own logic.</summary>
    public interface ILiveOpsClock
    {
        DateTime UtcNow { get; }
    }
}
