using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.UI.Navigation;

namespace GameFramework.Analytics.Integration
{
    /// <summary>
    /// Optional UI Navigation -&gt; Analytics bridge - see CLAUDE.md's Phase 16 brief, sections 17/67.
    /// Screen views are always tracked; popup open/close is opt-in via <paramref name="trackPopups"/>
    /// (constructor parameter) since "avoid tracking every popup automatically unless explicitly
    /// configured" (section 67). Opt-in, not bootstrapper-registered - see
    /// <see cref="GameFlowAnalyticsIntegration"/>'s remarks for the pattern.
    /// </summary>
    public sealed class NavigationAnalyticsIntegration : IDisposable
    {
        private readonly IEventService _events;
        private readonly IAnalyticsService _analytics;
        private readonly IDiagnosticsService _diagnostics;
        private readonly bool _trackPopups;

        public NavigationAnalyticsIntegration(
            IEventService events, IAnalyticsService analytics, IDiagnosticsService diagnostics = null, bool trackPopups = false)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _diagnostics = diagnostics;
            _trackPopups = trackPopups;

            _events.Subscribe<ScreenNavigatedEvent>(OnScreenNavigated);

            if (_trackPopups)
            {
                _events.Subscribe<PopupOpenedEvent>(OnPopupOpened);
                _events.Subscribe<PopupClosedEvent>(OnPopupClosed);
            }
        }

        public void Dispose()
        {
            _events.Unsubscribe<ScreenNavigatedEvent>(OnScreenNavigated);
            _events.Unsubscribe<PopupOpenedEvent>(OnPopupOpened);
            _events.Unsubscribe<PopupClosedEvent>(OnPopupClosed);
        }

        private void OnScreenNavigated(ScreenNavigatedEvent evt)
        {
            _diagnostics?.SetContext("screen", evt.Current.Value);
            _diagnostics?.AddBreadcrumb("Navigation", $"screen:{evt.Current}");
            _analytics.TrackScreenView(evt.Current.Value);
        }

        private void OnPopupOpened(PopupOpenedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Navigation", $"popup_opened:{evt.Id}");
            _analytics.Track("popup_opened", new Dictionary<string, object> { ["popup_id"] = evt.Id.Value });
        }

        private void OnPopupClosed(PopupClosedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Navigation", $"popup_closed:{evt.Id}");
            _analytics.Track("popup_closed", new Dictionary<string, object>
            {
                ["popup_id"] = evt.Id.Value,
                ["result"] = evt.Result.ToString()
            });
        }
    }
}
