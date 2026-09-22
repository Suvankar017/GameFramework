namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>This framework's own diagnostic version tag - independent of a game's
    /// <c>UnityEngine.Application.version</c>, which <see cref="DiagnosticsService"/> also records
    /// separately under its own context key (CLAUDE.md's Phase 16 brief, section 55).</summary>
    internal static class FrameworkInfo
    {
        public const string Version = "16.0";
    }
}
