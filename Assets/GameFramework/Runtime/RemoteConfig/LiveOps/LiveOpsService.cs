using System;
using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>
    /// Default <see cref="ILiveOpsService"/>. Layers an optional remote override on top of each
    /// authored <see cref="LiveEventDefinition"/> using three reserved, per-event
    /// <see cref="IRemoteConfigService"/> keys - see CLAUDE.md's Phase 17 brief, sections 6/46/58
    /// applied specifically to Live Ops scheduling:
    /// <code>
    /// liveops.{id}.enabled     (bool)   - overrides LiveEventDefinition.Enabled
    /// liveops.{id}.start_utc   (string) - overrides the authored start time, round-trip UTC format
    /// liveops.{id}.end_utc     (string) - overrides the authored end time, round-trip UTC format
    /// </code>
    /// These are ordinary ad hoc <see cref="IRemoteConfigService"/> keys (no
    /// <see cref="RemoteConfigDefinition"/> needs to declare them); a remote payload that omits one
    /// simply leaves that event's authored local value in effect, the same fallback-per-key precedence
    /// every other configuration value uses.
    ///
    /// Effective schedules are recomputed on every <see cref="IRemoteConfigService.ConfigurationActivated"/>
    /// and on a low-frequency poll (<see cref="LiveOpsConfiguration.PollIntervalSeconds"/>, via
    /// <see cref="ITimerService"/> - not a per-frame <c>Update</c>, see CLAUDE.md's Tick Rules) so a
    /// pure time-based Upcoming→Active→Ended transition is still detected between configuration
    /// changes.
    /// </summary>
    public sealed class LiveOpsService : ILiveOpsService
    {
        private const string KeyPrefix = "liveops.";

        private readonly LiveOpsConfiguration _configuration;
        private readonly List<LiveEventDefinition> _definitions = new List<LiveEventDefinition>();
        private readonly List<LiveEventId> _ids = new List<LiveEventId>();
        private readonly Dictionary<string, LiveEvent> _effective = new Dictionary<string, LiveEvent>(StringComparer.Ordinal);
        private readonly Dictionary<string, LiveEventState> _lastKnownState = new Dictionary<string, LiveEventState>(StringComparer.Ordinal);

        private IRemoteConfigService _remoteConfig;
        private IEventService _events;
        private ITimerService _timer;
        private ILiveOpsClock _clock;
        private ITimerHandle _pollHandle;

        public event Action<LiveEventId> LiveEventStarted;
        public event Action<LiveEventId> LiveEventEnded;

        public LiveOpsService(LiveOpsConfiguration configuration, ILiveOpsClock clock = null)
        {
            _configuration = configuration;
            _clock = clock;

            if (_configuration != null)
            {
                foreach (LiveEventDefinition definition in _configuration.Events)
                {
                    if (definition != null && definition.Id.IsValid)
                    {
                        _definitions.Add(definition);
                        _ids.Add(definition.Id);
                    }
                }
            }
        }

        public void Initialize(IServiceRegistry registry)
        {
            _remoteConfig = registry.Get<IRemoteConfigService>();
            _events = registry.Get<IEventService>();
            _timer = registry.Get<ITimerService>();
            _clock ??= new RemoteConfigLiveOpsClock(_remoteConfig);

            RecomputeAll();

            _remoteConfig.ConfigurationActivated += OnConfigurationActivated;

            float pollInterval = _configuration != null ? _configuration.PollIntervalSeconds : 15f;
            _pollHandle = _timer.StartRepeating(pollInterval, RecomputeAll, repeatCount: 0, TimerTimeMode.Unscaled);
        }

        public void Shutdown()
        {
            _remoteConfig.ConfigurationActivated -= OnConfigurationActivated;
            _pollHandle?.Cancel();
            _pollHandle = null;
        }

        public IReadOnlyList<LiveEventId> GetEventIds() => _ids;

        public bool TryGetEvent(LiveEventId id, out LiveEvent liveEvent) =>
            _effective.TryGetValue(id.Value ?? string.Empty, out liveEvent);

        public LiveEventState GetState(LiveEventId id) =>
            TryGetEvent(id, out LiveEvent liveEvent) ? liveEvent.ResolveState(_clock.UtcNow) : LiveEventState.Invalid;

        public bool IsActive(LiveEventId id) => GetState(id) == LiveEventState.Active;

        public IReadOnlyList<LiveEventId> GetActiveEvents()
        {
            var active = new List<LiveEventId>();
            for (int i = 0; i < _ids.Count; i++)
            {
                if (IsActive(_ids[i]))
                {
                    active.Add(_ids[i]);
                }
            }
            return active;
        }

        public LiveOpsDiagnostics GetDiagnostics()
        {
            int activeCount = 0;
            int upcomingCount = 0;
            DateTime now = _clock.UtcNow;

            foreach (LiveEvent liveEvent in _effective.Values)
            {
                LiveEventState state = liveEvent.ResolveState(now);
                if (state == LiveEventState.Active) activeCount++;
                else if (state == LiveEventState.Upcoming) upcomingCount++;
            }

            return new LiveOpsDiagnostics(_ids.Count, activeCount, upcomingCount);
        }

        private void OnConfigurationActivated(RemoteConfigSnapshot snapshot) => RecomputeAll();

        private void RecomputeAll()
        {
            DateTime now = _clock.UtcNow;

            for (int i = 0; i < _definitions.Count; i++)
            {
                LiveEventDefinition definition = _definitions[i];
                LiveEvent liveEvent = ResolveEffective(definition);
                _effective[definition.Id.Value] = liveEvent;

                LiveEventState newState = liveEvent.ResolveState(now);
                _lastKnownState.TryGetValue(definition.Id.Value, out LiveEventState previousState);

                if (newState == previousState)
                {
                    continue;
                }

                _lastKnownState[definition.Id.Value] = newState;

                if (newState == LiveEventState.Active)
                {
                    LiveEventStarted?.Invoke(definition.Id);
                    _events.Publish(new LiveEventStartedEvent(definition.Id));
                }
                else if (previousState == LiveEventState.Active)
                {
                    LiveEventEnded?.Invoke(definition.Id);
                    _events.Publish(new LiveEventEndedEvent(definition.Id));
                }
            }
        }

        private LiveEvent ResolveEffective(LiveEventDefinition definition)
        {
            string id = definition.Id.Value;
            bool enabled = _remoteConfig.GetBool(KeyPrefix + id + ".enabled", definition.Enabled);

            bool hasStart = definition.TryGetStartUtc(out DateTime start);
            bool hasEnd = definition.TryGetEndUtc(out DateTime end);

            string startOverride = _remoteConfig.GetString(KeyPrefix + id + ".start_utc", string.Empty);
            if (!string.IsNullOrEmpty(startOverride) && LiveEventDefinition.TryParse(startOverride, out DateTime overriddenStart))
            {
                start = overriddenStart;
                hasStart = true;
            }

            string endOverride = _remoteConfig.GetString(KeyPrefix + id + ".end_utc", string.Empty);
            if (!string.IsNullOrEmpty(endOverride) && LiveEventDefinition.TryParse(endOverride, out DateTime overriddenEnd))
            {
                end = overriddenEnd;
                hasEnd = true;
            }

            bool isValid = hasStart && hasEnd && end > start;

            return new LiveEvent(
                definition.Id, enabled, start, end,
                definition.TitleLocalizationKey, definition.DescriptionLocalizationKey, definition.IconId, definition.ConfigKeyPrefix,
                isValid);
        }
    }
}
