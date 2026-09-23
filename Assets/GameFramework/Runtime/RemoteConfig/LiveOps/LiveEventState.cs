namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>See CLAUDE.md's Phase 17 brief, section 38. Distinct from "remote config
    /// unavailable" - an event with an unreachable provider simply keeps evaluating against its last
    /// good/authored schedule, exactly like any other configuration value's fallback.</summary>
    public enum LiveEventState
    {
        Upcoming,
        Active,
        Ended,

        /// <summary>Explicitly disabled, locally or via remote override - never evaluated against its
        /// schedule.</summary>
        Disabled,

        /// <summary>Malformed schedule (e.g. an unparsable start/end time, or end before start) - see
        /// CLAUDE.md's Phase 17 brief, section 41's "reject invalid values" reasoning applied to Live
        /// Ops scheduling.</summary>
        Invalid
    }
}
