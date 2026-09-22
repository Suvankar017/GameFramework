namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>Does nothing - the default provider when no crash-reporting SDK is installed (see
    /// CLAUDE.md's Phase 16 brief, section 48). <see cref="DiagnosticsService"/> still logs every
    /// exception/error through the existing logger and keeps local breadcrumbs/context/tags even with
    /// this provider - only the remote report is skipped.</summary>
    public sealed class NoOpCrashReportingProvider : ICrashReportingProvider
    {
        public void Initialize()
        {
        }

        public void Report(DiagnosticReport report)
        {
        }

        public void SetUserId(string userId)
        {
        }
    }
}
