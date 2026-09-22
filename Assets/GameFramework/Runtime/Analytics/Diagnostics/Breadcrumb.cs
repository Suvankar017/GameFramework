using System;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>One entry in a <see cref="BreadcrumbRingBuffer"/> - a small note about what happened
    /// shortly before an error, e.g. "SceneLoaded"/"LevelStarted" (CLAUDE.md's Phase 16 brief,
    /// section 31).</summary>
    public readonly struct Breadcrumb
    {
        public readonly string Category;
        public readonly string Message;
        public readonly DateTime TimestampUtc;

        public Breadcrumb(string category, string message, DateTime timestampUtc)
        {
            Category = category;
            Message = message;
            TimestampUtc = timestampUtc;
        }
    }
}
