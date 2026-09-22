using System;
using System.Collections.Generic;

namespace GameFramework.Analytics
{
    /// <summary>
    /// Provider-independent analytics event - see CLAUDE.md's Phase 16 brief, section 6.
    /// <see cref="Parameters"/> is null (not an empty dictionary) when the event carries none, to
    /// avoid an allocation for the common parameterless case. Already validated/sanitized by
    /// <see cref="AnalyticsService"/> by the time a provider sees one - see
    /// <see cref="Providers.IAnalyticsProvider"/>'s remarks on the allowed parameter value types.
    /// </summary>
    public readonly struct AnalyticsEvent
    {
        public readonly string Name;
        public readonly IReadOnlyDictionary<string, object> Parameters;
        public readonly DateTime TimestampUtc;

        public AnalyticsEvent(string name, IReadOnlyDictionary<string, object> parameters, DateTime timestampUtc)
        {
            Name = name;
            Parameters = parameters;
            TimestampUtc = timestampUtc;
        }
    }
}
