namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>Structured error category - see CLAUDE.md's Phase 16 brief, section 58. Allows
    /// <see cref="ErrorCategory.Unknown"/> deliberately rather than forcing every exception into an
    /// incorrect bucket.</summary>
    public enum ErrorCategory
    {
        Unknown,
        Gameplay,
        Asset,
        Scene,
        UI,
        Audio,
        Input,
        Persistence,
        Monetization,
        Network,
        Platform,
        Configuration,

        /// <summary>The framework's own self-diagnostics (e.g. a provider/service initialization
        /// failure) - see CLAUDE.md's Phase 16 brief, section 59.</summary>
        Framework
    }
}
