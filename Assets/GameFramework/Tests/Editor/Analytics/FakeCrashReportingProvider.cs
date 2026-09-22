using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;

namespace GameFramework.Analytics.Tests
{
    /// <summary>Fully-controllable test double, distinct from the shipped
    /// <see cref="Diagnostics.Mock.MockCrashReportingProvider"/> - see
    /// <see cref="FakeAnalyticsProvider"/>'s remarks for why the two roles are kept separate.</summary>
    internal sealed class FakeCrashReportingProvider : ICrashReportingProvider
    {
        public readonly List<DiagnosticReport> ReceivedReports = new List<DiagnosticReport>();
        public string UserId;
        public int ReportCallCount;
        public bool ThrowOnReport;

        public void Initialize()
        {
        }

        public void Report(DiagnosticReport report)
        {
            ReportCallCount++;

            if (ThrowOnReport)
            {
                throw new InvalidOperationException("Simulated provider failure.");
            }

            ReceivedReports.Add(report);
        }

        public void SetUserId(string userId) => UserId = userId;
    }
}
