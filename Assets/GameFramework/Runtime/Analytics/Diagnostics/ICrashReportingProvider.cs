namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>
    /// Crash/error-reporting provider adapter seam - see CLAUDE.md's Phase 16 brief, sections 30/45.
    /// An implementation wraps exactly one external SDK (Crashlytics, Sentry, ...); none is installed
    /// in this project - see <see cref="NoOpCrashReportingProvider"/>'s remarks.
    ///
    /// <see cref="DiagnosticsService"/> wraps every call in a try/catch and, on failure, logs through
    /// the plain logger only - never by calling back into this same provider (see
    /// <see cref="DiagnosticsService"/>'s remarks on preventing recursive diagnostics, CLAUDE.md's
    /// Phase 16 brief, section 83).
    /// </summary>
    public interface ICrashReportingProvider
    {
        void Initialize();

        void Report(DiagnosticReport report);

        void SetUserId(string userId);
    }
}
