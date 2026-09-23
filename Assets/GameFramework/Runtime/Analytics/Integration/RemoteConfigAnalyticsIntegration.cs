using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.RemoteConfig;
using GameFramework.RemoteConfig.FeatureFlags;
using GameFramework.RemoteConfig.LiveOps;
using GameFramework.Runtime.Events;

namespace GameFramework.Analytics.Integration
{
    /// <summary>
    /// Optional Remote Config/Live Ops -&gt; Analytics bridge - see CLAUDE.md's Phase 17 brief, section
    /// 52. Consumes Phase 17's own published events only, never an analytics/diagnostics provider
    /// directly (Phase 17 "does not directly call an analytics provider; it emits framework events" -
    /// this bridge is what Phase 16 listens with). Opt-in, not bootstrapper-registered - see
    /// <see cref="GameFlowAnalyticsIntegration"/>'s remarks for the pattern; depends only on the event
    /// types it subscribes to, so it works whether or not <see cref="IFeatureFlagService"/>/
    /// <see cref="ILiveOpsService"/> end up registered in a given game.
    /// </summary>
    public sealed class RemoteConfigAnalyticsIntegration : IDisposable
    {
        private readonly IEventService _events;
        private readonly IAnalyticsService _analytics;
        private readonly IDiagnosticsService _diagnostics;

        public RemoteConfigAnalyticsIntegration(IEventService events, IAnalyticsService analytics, IDiagnosticsService diagnostics = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _diagnostics = diagnostics;

            _events.Subscribe<ConfigFetchFailedEvent>(OnConfigFetchFailed);
            _events.Subscribe<ConfigActivatedEvent>(OnConfigActivated);
            _events.Subscribe<ConfigRejectedEvent>(OnConfigRejected);
            _events.Subscribe<FeatureFlagChangedEvent>(OnFeatureFlagChanged);
            _events.Subscribe<LiveEventStartedEvent>(OnLiveEventStarted);
            _events.Subscribe<LiveEventEndedEvent>(OnLiveEventEnded);
        }

        public void Dispose()
        {
            _events.Unsubscribe<ConfigFetchFailedEvent>(OnConfigFetchFailed);
            _events.Unsubscribe<ConfigActivatedEvent>(OnConfigActivated);
            _events.Unsubscribe<ConfigRejectedEvent>(OnConfigRejected);
            _events.Unsubscribe<FeatureFlagChangedEvent>(OnFeatureFlagChanged);
            _events.Unsubscribe<LiveEventStartedEvent>(OnLiveEventStarted);
            _events.Unsubscribe<LiveEventEndedEvent>(OnLiveEventEnded);
        }

        private void OnConfigFetchFailed(ConfigFetchFailedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("RemoteConfig", $"fetch_failed:{evt.FailureDetail}");
            _analytics.Track(EventNames.ConfigFetchFailed, new Dictionary<string, object> { ["reason"] = evt.FailureDetail ?? string.Empty });
        }

        private void OnConfigActivated(ConfigActivatedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("RemoteConfig", $"activated:v{evt.Snapshot.Version}");
            _analytics.Track(EventNames.ConfigActivated, new Dictionary<string, object>
            {
                ["version"] = evt.Snapshot.Version,
                ["schema_version"] = evt.Snapshot.SchemaVersion
            });
        }

        private void OnConfigRejected(ConfigRejectedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("RemoteConfig", $"rejected:{evt.FailureDetail}");
            _analytics.Track(EventNames.ConfigRejected, new Dictionary<string, object> { ["reason"] = evt.FailureDetail ?? string.Empty });
        }

        private void OnFeatureFlagChanged(FeatureFlagChangedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("RemoteConfig", $"flag_changed:{evt.Key}={evt.IsEnabled}");
            _analytics.Track(EventNames.FeatureFlagChanged, new Dictionary<string, object> { ["key"] = evt.Key, ["enabled"] = evt.IsEnabled });
        }

        private void OnLiveEventStarted(LiveEventStartedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("RemoteConfig", $"live_event_started:{evt.Id}");
            _analytics.Track(EventNames.LiveEventStarted, new Dictionary<string, object> { ["event_id"] = evt.Id.Value });
        }

        private void OnLiveEventEnded(LiveEventEndedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("RemoteConfig", $"live_event_ended:{evt.Id}");
            _analytics.Track(EventNames.LiveEventEnded, new Dictionary<string, object> { ["event_id"] = evt.Id.Value });
        }
    }
}
