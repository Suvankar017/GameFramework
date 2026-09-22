using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Tutorials;

namespace GameFramework.Analytics.Integration
{
    /// <summary>Optional Tutorial -&gt; Analytics bridge - see CLAUDE.md's Phase 16 brief, sections
    /// 21/65. Only reports events already published by <c>ITutorialService</c>; never controls
    /// tutorial execution. Opt-in, not bootstrapper-registered - see
    /// <see cref="GameFlowAnalyticsIntegration"/>'s remarks for the pattern.</summary>
    public sealed class TutorialAnalyticsIntegration : IDisposable
    {
        private readonly IEventService _events;
        private readonly IAnalyticsService _analytics;
        private readonly IDiagnosticsService _diagnostics;

        public TutorialAnalyticsIntegration(IEventService events, IAnalyticsService analytics, IDiagnosticsService diagnostics = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _diagnostics = diagnostics;

            _events.Subscribe<TutorialStartedEvent>(OnStarted);
            _events.Subscribe<TutorialCompletedEvent>(OnCompleted);
            _events.Subscribe<TutorialSkippedEvent>(OnSkipped);
            _events.Subscribe<TutorialCancelledEvent>(OnCancelled);
        }

        public void Dispose()
        {
            _events.Unsubscribe<TutorialStartedEvent>(OnStarted);
            _events.Unsubscribe<TutorialCompletedEvent>(OnCompleted);
            _events.Unsubscribe<TutorialSkippedEvent>(OnSkipped);
            _events.Unsubscribe<TutorialCancelledEvent>(OnCancelled);
        }

        private void OnStarted(TutorialStartedEvent evt) => TrackTutorial(EventNames.TutorialStarted, evt.TutorialId, "tutorial_started");
        private void OnCompleted(TutorialCompletedEvent evt) => TrackTutorial(EventNames.TutorialCompleted, evt.TutorialId, "tutorial_completed");
        private void OnSkipped(TutorialSkippedEvent evt) => TrackTutorial(EventNames.TutorialSkipped, evt.TutorialId, "tutorial_skipped");
        private void OnCancelled(TutorialCancelledEvent evt) => TrackTutorial(EventNames.TutorialCancelled, evt.TutorialId, "tutorial_cancelled");

        private void TrackTutorial(string eventName, TutorialId id, string breadcrumbMessage)
        {
            _diagnostics?.AddBreadcrumb("Tutorial", $"{breadcrumbMessage}:{id}");
            _analytics.Track(eventName, new Dictionary<string, object> { ["tutorial_id"] = id.Value });
        }
    }
}
