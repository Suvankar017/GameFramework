using System;
using System.Collections.Generic;

namespace GameFramework.RemoteConfig.Providers
{
    /// <summary>
    /// A provider's raw fetch outcome - see CLAUDE.md's Phase 17 brief, sections 12/21. Conversion
    /// from whatever wire format a real SDK returns (JSON, a platform-native dictionary, ...) happens
    /// entirely inside the provider; <see cref="Values"/> is already boxed
    /// bool/int/long/float/double/string, the same convention <c>AnalyticsEvent.Parameters</c> uses -
    /// <see cref="RemoteConfigService"/> never parses a wire format itself.
    /// </summary>
    public readonly struct RemoteConfigProviderResult
    {
        public readonly bool Success;
        public readonly int Version;
        public readonly int SchemaVersion;
        public readonly IReadOnlyDictionary<string, object> Values;

        /// <summary>The provider's server-reported time at the moment of this fetch, if it supplies
        /// one - see CLAUDE.md's Phase 17 brief, sections 39-41. Null means "no server time available
        /// from this provider"; <see cref="RemoteConfigService.ServerTimeOffset"/> stays zero and Live
        /// Ops scheduling explicitly falls back to local time.</summary>
        public readonly DateTime? ServerTimeUtc;

        public readonly string FailureDetail;

        private RemoteConfigProviderResult(bool success, int version, int schemaVersion, IReadOnlyDictionary<string, object> values, DateTime? serverTimeUtc, string failureDetail)
        {
            Success = success;
            Version = version;
            SchemaVersion = schemaVersion;
            Values = values;
            ServerTimeUtc = serverTimeUtc;
            FailureDetail = failureDetail;
        }

        public static RemoteConfigProviderResult Successful(int version, int schemaVersion, IReadOnlyDictionary<string, object> values, DateTime? serverTimeUtc = null) =>
            new RemoteConfigProviderResult(true, version, schemaVersion, values, serverTimeUtc, null);

        public static RemoteConfigProviderResult Failed(string failureDetail) =>
            new RemoteConfigProviderResult(false, 0, 0, null, null, failureDetail);
    }
}
