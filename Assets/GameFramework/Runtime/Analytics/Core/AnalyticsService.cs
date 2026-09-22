using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Analytics.Providers;
using GameFramework.Performance.Mobile;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using UnityEngine;
using Log = GameFramework.Runtime.Diagnostics.Log;

namespace GameFramework.Analytics
{
    /// <summary>
    /// Default <see cref="IAnalyticsService"/> implementation - see CLAUDE.md's Phase 16 brief for
    /// the full design. Owns event validation/sanitization, consent-gated buffering, session
    /// lifecycle (built on <see cref="Performance.Mobile.ApplicationPausedEvent"/>/
    /// <see cref="ApplicationResumedEvent"/>/<see cref="ApplicationQuittingEvent"/> - the same
    /// "subscribe directly, no game required to have Performance's lifecycle service registered"
    /// pattern <c>Monetization.Ads.AdsService</c> already established), and anonymous identity
    /// (persisted directly via <c>IPersistenceService</c>, independent of <c>PlayerData</c> - see
    /// CLAUDE.md's Phase 16 brief, section 68).
    ///
    /// Never throws out of a public call - a provider failure is caught, logged through the plain
    /// logger, and (if <see cref="IDiagnosticsService"/> is registered) recorded as a framework
    /// diagnostic; it is never allowed to propagate and break gameplay (section 82).
    /// </summary>
    public sealed class AnalyticsService : IAnalyticsService
    {
        private const string IdentityPersistenceKey = "GameFramework.Analytics.Identity";
        private const int IdentitySaveVersion = 1;
        private const string LogCategory = "Analytics";

        private readonly AnalyticsConfiguration _configuration;
        private readonly IAnalyticsProvider _provider;

        private IEventService _events;
        private IPersistenceService _persistence;
        private IDiagnosticsService _diagnostics;

        private readonly List<AnalyticsEvent> _pendingQueue = new List<AnalyticsEvent>();

        private bool _isEnabled;
        private ConsentState _consent = ConsentState.Unknown;
        private string _userId = string.Empty;
        private string _sessionId = string.Empty;
        private DateTime? _backgroundedAtUtc;
        private string _lastEventName = string.Empty;
        private int _eventsSentCount;

        public AnalyticsService(AnalyticsConfiguration configuration, IAnalyticsProvider provider)
        {
            _configuration = configuration != null ? configuration : ScriptableObject.CreateInstance<AnalyticsConfiguration>();
            _provider = provider ?? new NoOpAnalyticsProvider();
        }

        public bool IsEnabled => _isEnabled;
        public ConsentState Consent => _consent;
        public string UserId => _userId;
        public string SessionId => _sessionId;

        public event Action<ConsentState> ConsentChanged;

        public void Initialize(IServiceRegistry registry)
        {
            _events = registry.Get<IEventService>();
            _persistence = registry.Get<IPersistenceService>();
            registry.TryGet(out _diagnostics);

            _isEnabled = _configuration.EnabledByDefault;
            _consent = _configuration.ConsentRequired ? ConsentState.Unknown : ConsentState.Granted;

            LoadOrCreateIdentity();

            try
            {
                _provider.Initialize();
                _provider.SetUserId(_userId);
                _provider.SetConsent(_consent == ConsentState.Granted);
            }
            catch (Exception ex)
            {
                ReportProviderFailure(ex);
            }

            StartSession();

            _events.Subscribe<ApplicationPausedEvent>(OnApplicationPaused);
            _events.Subscribe<ApplicationResumedEvent>(OnApplicationResumed);
            _events.Subscribe<ApplicationQuittingEvent>(OnApplicationQuitting);
        }

        public void Shutdown()
        {
            _events.Unsubscribe<ApplicationPausedEvent>(OnApplicationPaused);
            _events.Unsubscribe<ApplicationResumedEvent>(OnApplicationResumed);
            _events.Unsubscribe<ApplicationQuittingEvent>(OnApplicationQuitting);

            EndSession(publishEvent: false);
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public void SetConsent(ConsentState consent)
        {
            if (_consent == consent)
            {
                return;
            }

            ConsentState previous = _consent;
            _consent = consent;

            try
            {
                _provider.SetConsent(consent == ConsentState.Granted);
            }
            catch (Exception ex)
            {
                ReportProviderFailure(ex);
            }

            if (consent == ConsentState.Granted)
            {
                FlushInternal();
            }
            else if (consent == ConsentState.Denied)
            {
                // Privacy takes precedence over completeness - a previously buffered event is never
                // sent once the player has explicitly denied consent (CLAUDE.md's Phase 16 brief,
                // section 37).
                _pendingQueue.Clear();
            }

            _events.Publish(new ConsentChangedEvent(previous, consent));
            ConsentChanged?.Invoke(consent);
        }

        public void ResetIdentity()
        {
            _userId = Guid.NewGuid().ToString("N");
            SaveIdentity();

            try
            {
                _provider.SetUserId(_userId);
            }
            catch (Exception ex)
            {
                ReportProviderFailure(ex);
            }
        }

        public void Track(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (!_isEnabled)
            {
                return;
            }

            if (!IsValidEventName(eventName, out string invalidReason))
            {
                Log.Warning(LogCategory, $"Dropped event with invalid name '{eventName}': {invalidReason}.");
                return;
            }

            IReadOnlyDictionary<string, object> sanitized = SanitizeParameters(eventName, parameters);
            _lastEventName = eventName;
            Dispatch(new AnalyticsEvent(eventName, sanitized, DateTime.UtcNow));
        }

        public void TrackScreenView(string screenName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(screenName))
            {
                return;
            }

            var merged = parameters != null ? new Dictionary<string, object>(parameters) : new Dictionary<string, object>();
            merged["screen_name"] = screenName;
            Track(EventNames.ScreenView, merged);
        }

        public void SetUserProperty(string key, object value)
        {
            if (!_isEnabled || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!IsSupportedParameterValue(value))
            {
                Log.Warning(LogCategory, $"Dropped user property '{key}': unsupported value type.");
                return;
            }

            try
            {
                _provider.SetUserProperty(key, value);
            }
            catch (Exception ex)
            {
                ReportProviderFailure(ex);
            }
        }

        public void Flush() => FlushInternal();

        public AnalyticsDiagnostics GetDiagnostics() => new AnalyticsDiagnostics(
            _isEnabled, _consent, _provider.GetType().Name, _sessionId, _userId, _eventsSentCount, _pendingQueue.Count, _lastEventName);

        private void Dispatch(AnalyticsEvent analyticsEvent)
        {
            switch (_consent)
            {
                case ConsentState.Denied:
                    return;

                case ConsentState.Unknown:
                    if (_configuration.ConsentPolicy == ConsentPolicy.BufferUntilDecided)
                    {
                        Enqueue(analyticsEvent);
                    }
                    return;

                default: // Granted
                    Send(analyticsEvent);
                    return;
            }
        }

        private void Enqueue(AnalyticsEvent analyticsEvent)
        {
            if (_pendingQueue.Count >= _configuration.EventQueueCapacity)
            {
                _pendingQueue.RemoveAt(0);
            }

            _pendingQueue.Add(analyticsEvent);
        }

        private void Send(AnalyticsEvent analyticsEvent)
        {
            try
            {
                _provider.TrackEvent(analyticsEvent);
                _eventsSentCount++;
            }
            catch (Exception ex)
            {
                ReportProviderFailure(ex);
            }
        }

        private void FlushInternal()
        {
            if (_consent != ConsentState.Granted || _pendingQueue.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingQueue.Count; i++)
            {
                Send(_pendingQueue[i]);
            }

            _pendingQueue.Clear();
        }

        private void ReportProviderFailure(Exception ex)
        {
            Log.Exception(ex, LogCategory);
            _diagnostics?.RecordException(ex, ErrorCategory.Framework);
        }

        private void StartSession()
        {
            _sessionId = Guid.NewGuid().ToString("N");
            _backgroundedAtUtc = null;
            _diagnostics?.SetContext("session_id", _sessionId);
            Track(EventNames.SessionStart);
        }

        private void EndSession(bool publishEvent)
        {
            if (publishEvent)
            {
                Track(EventNames.SessionEnd);
            }

            FlushInternal();
        }

        private void OnApplicationPaused(ApplicationPausedEvent evt) => _backgroundedAtUtc = DateTime.UtcNow;

        private void OnApplicationResumed(ApplicationResumedEvent evt)
        {
            if (!_backgroundedAtUtc.HasValue)
            {
                return;
            }

            double backgroundedSeconds = (DateTime.UtcNow - _backgroundedAtUtc.Value).TotalSeconds;
            _backgroundedAtUtc = null;

            if (backgroundedSeconds >= _configuration.SessionTimeoutSeconds)
            {
                EndSession(publishEvent: true);
                StartSession();
            }
        }

        // A mobile process can be killed by the OS without ever receiving this - see CLAUDE.md's
        // Phase 16 brief, section 16. This is a best-effort session close, not a guarantee.
        private void OnApplicationQuitting(ApplicationQuittingEvent evt) => EndSession(publishEvent: true);

        private void LoadOrCreateIdentity()
        {
            AnalyticsIdentityData data = _persistence.Load(IdentityPersistenceKey, IdentitySaveVersion, new AnalyticsIdentityData());

            if (!string.IsNullOrEmpty(data.UserId))
            {
                _userId = data.UserId;
                return;
            }

            _userId = Guid.NewGuid().ToString("N");
            SaveIdentity();
        }

        private void SaveIdentity() =>
            _persistence.Save(IdentityPersistenceKey, new AnalyticsIdentityData { UserId = _userId }, IdentitySaveVersion);

        private bool IsValidEventName(string name, out string reason)
        {
            if (string.IsNullOrEmpty(name))
            {
                reason = "empty";
                return false;
            }

            if (name.Length > _configuration.MaxEventNameLength)
            {
                reason = "too long";
                return false;
            }

            if (!char.IsLetter(name[0]))
            {
                reason = "must start with a letter";
                return false;
            }

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    reason = $"invalid character '{c}'";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        private IReadOnlyDictionary<string, object> SanitizeParameters(string eventName, IReadOnlyDictionary<string, object> raw)
        {
            if (raw == null || raw.Count == 0)
            {
                return null;
            }

            Dictionary<string, object> sanitized = null;
            bool droppedAny = false;

            foreach (KeyValuePair<string, object> kvp in raw)
            {
                if (sanitized != null && sanitized.Count >= _configuration.MaxParametersPerEvent)
                {
                    droppedAny = true;
                    break;
                }

                if (string.IsNullOrEmpty(kvp.Key) || kvp.Key.Length > _configuration.MaxParameterKeyLength)
                {
                    droppedAny = true;
                    continue;
                }

                object value = kvp.Value;
                if (value is string stringValue)
                {
                    if (stringValue.Length > _configuration.MaxParameterValueLength)
                    {
                        value = stringValue.Substring(0, _configuration.MaxParameterValueLength);
                    }
                }
                else if (!IsSupportedParameterValue(value))
                {
                    droppedAny = true;
                    continue;
                }

                sanitized ??= new Dictionary<string, object>();
                sanitized[kvp.Key] = value;
            }

            if (droppedAny)
            {
                Log.Warning(LogCategory, $"Event '{eventName}' had one or more parameters dropped during sanitization.");
            }

            return sanitized;
        }

        private static bool IsSupportedParameterValue(object value) =>
            value is string || value is int || value is long || value is float || value is double || value is bool;
    }
}
