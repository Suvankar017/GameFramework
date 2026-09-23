namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Informational classification of how safely a definition's value may be re-read while the game
    /// is running - see CLAUDE.md's Phase 17 brief, section 36. This framework does not enforce or
    /// act on this value; it activates a validated snapshot atomically regardless of policy, and it
    /// is purely up to each consumer to decide whether/when to re-read a value it cares about
    /// (section 36: "consumers decide whether a change can safely be applied at runtime").
    /// </summary>
    public enum RuntimeChangePolicy
    {
        /// <summary>Only read once, at startup (e.g. before the first scene loads). A later change is
        /// still activated in the snapshot, but a consumer following this policy ignores it until the
        /// next app launch.</summary>
        StartupOnly,

        /// <summary>Safe to re-read and apply the instant a new snapshot activates.</summary>
        SafeAtRuntime,

        /// <summary>Should only be re-read the next time a new session/profile starts.</summary>
        NextSession,

        /// <summary>Should only be re-read the next time a level/gameplay operation starts, to avoid
        /// an inconsistent mid-operation change (section 15/36).</summary>
        NextLevel
    }
}
