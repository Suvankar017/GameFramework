using System;
using System.Collections.Generic;
using System.Text;

namespace GameFramework.Editor.Build
{
    public enum BuildValidationSeverity
    {
        /// <summary>A check that ran and found nothing wrong.</summary>
        Pass,

        /// <summary>Informational only.</summary>
        Info,

        /// <summary>The build may continue but the problem must be visible.</summary>
        Warning,

        /// <summary>The build must not continue.</summary>
        Error
    }

    /// <summary>One validation result. <see cref="CheckId"/> is stable (e.g. <c>Android.Signing</c>)
    /// so a profile can promote that check's warnings to errors. Messages never contain secret
    /// values; at most they name an environment variable.</summary>
    [Serializable]
    public sealed class BuildValidationIssue
    {
        public string CheckId;
        public BuildValidationSeverity Severity;

        /// <summary><see cref="Severity"/> as text ("Error", ...) - JsonUtility writes enums as
        /// integers, and CI consumers of build-report.json should not need the enum's ordinal.</summary>
        public string Level;
        public string Message;
        public string SuggestedFix;

        /// <summary>True if this was a warning promoted by the profile's <c>WarningsAsErrors</c>.</summary>
        public bool Promoted;
    }

    /// <summary>
    /// Structured validation output. Validators add results; they never throw for a failed check.
    /// Rendering (<see cref="ToText"/>) is one line per result with <c>[PASS]</c>/<c>[INFO]</c>/
    /// <c>[WARN]</c>/<c>[FAIL]</c> prefixes, readable by a person and trivially greppable in CI logs.
    /// </summary>
    public sealed class BuildValidationReport
    {
        private readonly List<BuildValidationIssue> _issues = new List<BuildValidationIssue>();
        private readonly HashSet<string> _promotedCheckIds;

        public BuildValidationReport(IEnumerable<string> warningsAsErrors = null)
        {
            _promotedCheckIds = new HashSet<string>(warningsAsErrors ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public IReadOnlyList<BuildValidationIssue> Issues => _issues;

        public int ErrorCount => Count(BuildValidationSeverity.Error);
        public int WarningCount => Count(BuildValidationSeverity.Warning);
        public int InfoCount => Count(BuildValidationSeverity.Info);
        public int PassCount => Count(BuildValidationSeverity.Pass);
        public bool HasErrors => ErrorCount > 0;

        public void Pass(string checkId, string message) => Add(checkId, BuildValidationSeverity.Pass, message, null);
        public void Info(string checkId, string message) => Add(checkId, BuildValidationSeverity.Info, message, null);
        public void Warning(string checkId, string message, string fix = null) => Add(checkId, BuildValidationSeverity.Warning, message, fix);
        public void Error(string checkId, string message, string fix = null) => Add(checkId, BuildValidationSeverity.Error, message, fix);

        /// <summary>Error when <paramref name="isError"/>, otherwise a warning - for checks whose
        /// severity depends on the environment (e.g. fatal for Production, visible for Staging).</summary>
        public void ErrorOrWarning(bool isError, string checkId, string message, string fix = null) =>
            Add(checkId, isError ? BuildValidationSeverity.Error : BuildValidationSeverity.Warning, message, fix);

        public void Add(string checkId, BuildValidationSeverity severity, string message, string fix)
        {
            bool promote = severity == BuildValidationSeverity.Warning && checkId != null && _promotedCheckIds.Contains(checkId);
            BuildValidationSeverity effective = promote ? BuildValidationSeverity.Error : severity;
            _issues.Add(new BuildValidationIssue
            {
                CheckId = checkId ?? string.Empty,
                Severity = effective,
                Level = effective.ToString(),
                Message = message ?? string.Empty,
                SuggestedFix = fix ?? string.Empty,
                Promoted = promote
            });
        }

        public string ToText(string title)
        {
            var builder = new StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine();
            builder.Append("Errors: ").Append(ErrorCount)
                .Append("  Warnings: ").Append(WarningCount)
                .Append("  Info: ").Append(InfoCount)
                .Append("  Passed: ").Append(PassCount).AppendLine();
            builder.AppendLine();

            foreach (BuildValidationIssue issue in _issues)
            {
                builder.Append(Prefix(issue.Severity)).Append(' ').Append(issue.CheckId).Append(": ").Append(issue.Message);
                if (issue.Promoted)
                {
                    builder.Append(" (warning promoted to error by profile)");
                }

                builder.AppendLine();
                if (!string.IsNullOrEmpty(issue.SuggestedFix) && issue.Severity >= BuildValidationSeverity.Warning)
                {
                    builder.Append("       fix: ").AppendLine(issue.SuggestedFix);
                }
            }

            return builder.ToString();
        }

        public static string Prefix(BuildValidationSeverity severity)
        {
            switch (severity)
            {
                case BuildValidationSeverity.Pass: return "[PASS]";
                case BuildValidationSeverity.Info: return "[INFO]";
                case BuildValidationSeverity.Warning: return "[WARN]";
                default: return "[FAIL]";
            }
        }

        private int Count(BuildValidationSeverity severity)
        {
            int count = 0;
            foreach (BuildValidationIssue issue in _issues)
            {
                if (issue.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
