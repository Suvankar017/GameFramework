namespace GameFramework.Analytics
{
    /// <summary>Build/deployment environment tag - see CLAUDE.md's Phase 16 brief, section 53. Purely
    /// informational metadata a provider adapter or dashboard filter may use; this framework does not
    /// branch behavior on it beyond exposing it via <see cref="AnalyticsConfiguration.Environment"/>.</summary>
    public enum AnalyticsEnvironment
    {
        Development,
        Staging,
        Production
    }
}
