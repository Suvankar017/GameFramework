using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.GameFlow;
using GameFramework.Runtime.Events;

namespace GameFramework.Analytics.Integration
{
    /// <summary>
    /// Optional GameFlow -&gt; Analytics bridge - see CLAUDE.md's Phase 16 brief, sections 19/66.
    /// Analytics only ever *observes* <c>GameFlow.IGameFlowService</c> through its published events;
    /// it never calls into GameFlow. Like <c>Monetization.Integration.AdPlacementRewardBridge</c>,
    /// this is opt-in and not registered by <see cref="AnalyticsBootstrapper"/> - a game constructs
    /// one explicitly (after both Analytics and GameFlow are registered) and disposes it on shutdown:
    /// <code>
    /// var bridge = new GameFlowAnalyticsIntegration(
    ///     registry.Get&lt;IEventService&gt;(), registry.Get&lt;IAnalyticsService&gt;(), diagnostics);
    /// </code>
    /// </summary>
    public sealed class GameFlowAnalyticsIntegration : IDisposable
    {
        private readonly IEventService _events;
        private readonly IAnalyticsService _analytics;
        private readonly IDiagnosticsService _diagnostics;

        public GameFlowAnalyticsIntegration(IEventService events, IAnalyticsService analytics, IDiagnosticsService diagnostics = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _diagnostics = diagnostics;

            _events.Subscribe<LevelStartedEvent>(OnLevelStarted);
            _events.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Subscribe<LevelFailedEvent>(OnLevelFailed);
            _events.Subscribe<LevelRestartedEvent>(OnLevelRestarted);
            _events.Subscribe<CheckpointActivatedEvent>(OnCheckpointActivated);
        }

        public void Dispose()
        {
            _events.Unsubscribe<LevelStartedEvent>(OnLevelStarted);
            _events.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
            _events.Unsubscribe<LevelFailedEvent>(OnLevelFailed);
            _events.Unsubscribe<LevelRestartedEvent>(OnLevelRestarted);
            _events.Unsubscribe<CheckpointActivatedEvent>(OnCheckpointActivated);
        }

        private void OnLevelStarted(LevelStartedEvent evt)
        {
            _diagnostics?.SetContext("level", evt.LevelId.Value);
            _diagnostics?.AddBreadcrumb("GameFlow", $"level_started:{evt.LevelId}");
            _analytics.Track(EventNames.LevelStarted, new Dictionary<string, object>
            {
                ["level_id"] = evt.LevelId.Value,
                ["attempt_number"] = evt.AttemptNumber
            });
        }

        private void OnLevelCompleted(LevelCompletedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("GameFlow", $"level_completed:{evt.LevelId}");
            _analytics.Track(EventNames.LevelCompleted, new Dictionary<string, object>
            {
                ["level_id"] = evt.LevelId.Value,
                ["attempt_number"] = evt.AttemptNumber,
                ["completion_time"] = evt.CompletionTime,
                ["result"] = evt.Result.ToString()
            });
        }

        private void OnLevelFailed(LevelFailedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("GameFlow", $"level_failed:{evt.LevelId}");
            _analytics.Track(EventNames.LevelFailed, new Dictionary<string, object>
            {
                ["level_id"] = evt.LevelId.Value,
                ["attempt_number"] = evt.AttemptNumber,
                ["result"] = evt.Result.ToString()
            });
        }

        private void OnLevelRestarted(LevelRestartedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("GameFlow", $"level_restarted:{evt.LevelId}");
            _analytics.Track(EventNames.LevelRestarted, new Dictionary<string, object> { ["level_id"] = evt.LevelId.Value });
        }

        private void OnCheckpointActivated(CheckpointActivatedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("GameFlow", $"checkpoint_reached:{evt.CheckpointId}");
            _analytics.Track(EventNames.CheckpointReached, new Dictionary<string, object> { ["checkpoint_id"] = evt.CheckpointId });
        }
    }
}
