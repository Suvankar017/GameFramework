using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Game-facing Remote Config API - see CLAUDE.md's Phase 17 brief, section 78. A game should be
    /// able to write <c>remoteConfig.GetInt("economy.reward_multiplier", 1)</c> without knowing
    /// anything about the provider underneath, exactly as <c>Analytics.IAnalyticsService.Track</c>
    /// hides its provider.
    ///
    /// Every Get* method reads <see cref="ActiveSnapshot"/> only - see that property's remarks on
    /// atomicity. None of them ever throws for a missing/mistyped key; they return
    /// <paramref name="defaultValue"/> instead (section 55).
    /// </summary>
    public interface IRemoteConfigService : IGameService
    {
        RemoteConfigState State { get; }
        RemoteConfigEnvironment Environment { get; }

        /// <summary>The currently active, immutable configuration - never null (at minimum, the
        /// defaults-only snapshot built during <see cref="IGameService.Initialize"/>). See
        /// <see cref="RemoteConfigSnapshot"/>'s remarks on atomicity/immutability (section 77).</summary>
        RemoteConfigSnapshot ActiveSnapshot { get; }

        /// <summary>Every declared key's local schema - useful for editor tooling/diagnostics that
        /// need to enumerate what exists rather than reading one key at a time.</summary>
        IReadOnlyList<RemoteConfigDefinition> Definitions { get; }

        /// <summary>Local-time-to-server-time offset computed from the most recent fetch that
        /// supplied a server timestamp; <see cref="TimeSpan.Zero"/> if no provider has ever supplied
        /// one - see CLAUDE.md's Phase 17 brief, sections 39-41. <c>Live Ops</c> scheduling adds this
        /// to <see cref="DateTime.UtcNow"/> rather than trusting local time outright.</summary>
        TimeSpan ServerTimeOffset { get; }

        DateTime? LastFetchUtc { get; }
        DateTime? LastActivationUtc { get; }

        /// <summary>True once <see cref="LastFetchUtc"/> is more than
        /// <see cref="RemoteConfigConfiguration.StaleThresholdSeconds"/> in the past, or if there has
        /// never been a successful fetch/cache load at all.</summary>
        bool IsStale { get; }

        string LastError { get; }

        event Action<RemoteConfigState> StateChanged;

        /// <summary>Raised exactly once per successful activation - whether the new snapshot came
        /// from a fresh remote fetch or from a valid cached payload loaded at startup.</summary>
        event Action<RemoteConfigSnapshot> ConfigurationActivated;

        bool GetBool(string key, bool defaultValue = false);
        int GetInt(string key, int defaultValue = 0);
        long GetLong(string key, long defaultValue = 0L);
        float GetFloat(string key, float defaultValue = 0f);
        double GetDouble(string key, double defaultValue = 0d);
        string GetString(string key, string defaultValue = "");

        bool HasKey(string key);
        bool TryGetDefinition(string key, out RemoteConfigDefinition definition);

        /// <summary>
        /// Fetches, validates, and (if valid) activates the latest configuration. Rejected with
        /// <see cref="RemoteConfigFetchResultKind.AlreadyInProgress"/> if a fetch is already running -
        /// see CLAUDE.md's Phase 17 brief's own re-entrancy precedent. Never throws; every failure
        /// path (provider failure/unavailable, timeout, schema mismatch, validation failure) leaves
        /// whatever was previously active still active (section 55).
        /// </summary>
        void Fetch(Action<RemoteConfigFetchResult> onComplete = null);

        RemoteConfigDiagnostics GetDiagnostics();
    }
}
