using System;
using System.Collections.Generic;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>
    /// Immutable snapshot handed to <see cref="ICrashReportingProvider.Report"/> - bundles the
    /// exception/message, category, and a copy of the diagnostic context/tags/breadcrumbs at the
    /// moment of the report (CLAUDE.md's Phase 16 brief, section 34). <see cref="Exception"/> is null
    /// for a <see cref="IDiagnosticsService.RecordError"/> call (no exception object exists).
    /// </summary>
    public readonly struct DiagnosticReport
    {
        public readonly Exception Exception;
        public readonly string Message;
        public readonly ErrorCategory Category;
        public readonly DateTime TimestampUtc;
        public readonly IReadOnlyDictionary<string, string> Context;
        public readonly IReadOnlyDictionary<string, string> Tags;
        public readonly IReadOnlyList<Breadcrumb> Breadcrumbs;

        public DiagnosticReport(
            Exception exception,
            string message,
            ErrorCategory category,
            DateTime timestampUtc,
            IReadOnlyDictionary<string, string> context,
            IReadOnlyDictionary<string, string> tags,
            IReadOnlyList<Breadcrumb> breadcrumbs)
        {
            Exception = exception;
            Message = message;
            Category = category;
            TimestampUtc = timestampUtc;
            Context = context;
            Tags = tags;
            Breadcrumbs = breadcrumbs;
        }
    }
}
