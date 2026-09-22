using System.Collections.Generic;

namespace GameFramework.Analytics.Diagnostics.Mock
{
    /// <summary>Deterministic, Editor/test-safe provider that records every report in memory - see
    /// CLAUDE.md's Phase 16 brief, section 49. Never sends anything externally.</summary>
    public sealed class MockCrashReportingProvider : ICrashReportingProvider
    {
        public List<DiagnosticReport> ReceivedReports { get; } = new List<DiagnosticReport>();
        public string UserId { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }

        public void Report(DiagnosticReport report)
        {
            ReceivedReports.Add(report);
        }

        public void SetUserId(string userId)
        {
            UserId = userId;
        }
    }
}
