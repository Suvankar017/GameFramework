using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>
    /// Game-facing diagnostics/crash-reporting API - see CLAUDE.md's Phase 16 brief, sections 26-30.
    /// Distinct from <see cref="Runtime.Diagnostics.ILoggingService"/> (developer-readable console
    /// logging, unchanged by this phase) and from <see cref="IAnalyticsService"/> ("what happened in
    /// the game" vs. "what went wrong technically" - section 4). Named under
    /// <c>GameFramework.Analytics.Diagnostics</c>, not <c>GameFramework.Runtime.Diagnostics</c>, to
    /// avoid colliding with the existing logging namespace of the same short name.
    ///
    /// Every method here is safe to call regardless of whether a crash-reporting SDK is installed or
    /// <see cref="IsEnabled"/> is false - see <see cref="DiagnosticsService"/>'s remarks on failure
    /// isolation and CLAUDE.md's Phase 16 brief, section 83 (no recursive diagnostics).
    /// </summary>
    public interface IDiagnosticsService : IGameService
    {
        bool IsEnabled { get; }

        /// <summary>Appends a short breadcrumb to the bounded, in-memory history included in the
        /// next report - see CLAUDE.md's Phase 16 brief, section 31.</summary>
        void AddBreadcrumb(string category, string message);

        /// <summary>Sets/overwrites one entry of persistent diagnostic context (e.g. "level",
        /// "screen") included in every subsequent report - see CLAUDE.md's Phase 16 brief, section
        /// 32.</summary>
        void SetContext(string key, string value);

        /// <summary>Sets/overwrites one bounded diagnostic tag (e.g. "build_type") - see CLAUDE.md's
        /// Phase 16 brief, section 33.</summary>
        void SetTag(string key, string value);

        void SetUserId(string userId);

        /// <summary>Records an exception. Always logged through the existing logger regardless of
        /// <see cref="IsEnabled"/>; only forwarded to the crash-reporting provider when enabled.
        /// Never throws, including if the provider itself throws.</summary>
        void RecordException(Exception exception, ErrorCategory category = ErrorCategory.Unknown);

        /// <summary>Records a technical error with no associated exception object. Same guarantees as
        /// <see cref="RecordException"/>.</summary>
        void RecordError(string message, ErrorCategory category = ErrorCategory.Unknown);

        /// <summary>Snapshot of the current bounded breadcrumb history, oldest first.</summary>
        IReadOnlyList<Breadcrumb> GetBreadcrumbs();
    }
}
