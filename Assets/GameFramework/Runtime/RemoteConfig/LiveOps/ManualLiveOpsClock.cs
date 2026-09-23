using System;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>A settable <see cref="ILiveOpsClock"/> for QA/Editor simulation - see CLAUDE.md's
    /// Phase 17 brief, section 68 ("changing live event time"). Not used by
    /// <see cref="RemoteConfigBootstrapper"/> by default; a development-only tool that wants to
    /// preview an event before/during/after its window constructs <see cref="LiveOpsService"/> with
    /// one of these instead of the default clock.</summary>
    public sealed class ManualLiveOpsClock : ILiveOpsClock
    {
        public DateTime UtcNow { get; set; }

        public ManualLiveOpsClock(DateTime initialUtc)
        {
            UtcNow = initialUtc;
        }
    }
}
