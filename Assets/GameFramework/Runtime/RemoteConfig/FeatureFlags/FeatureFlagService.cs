using System;
using System.Collections.Generic;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;

namespace GameFramework.RemoteConfig.FeatureFlags
{
    /// <summary>
    /// Default <see cref="IFeatureFlagService"/>. Tracks a baseline resolved value for every declared
    /// boolean <see cref="RemoteConfigDefinition"/> and diffs it against each new activation to raise
    /// <see cref="FlagChanged"/>/publish <see cref="FeatureFlagChangedEvent"/> only for flags whose
    /// resolved value actually changed - see CLAUDE.md's Phase 17 brief, section 35.
    /// </summary>
    public sealed class FeatureFlagService : IFeatureFlagService
    {
        private readonly Dictionary<string, bool> _baseline = new Dictionary<string, bool>(StringComparer.Ordinal);

        private IRemoteConfigService _remoteConfig;
        private IEventService _events;

        public event Action<string, bool> FlagChanged;

        public void Initialize(IServiceRegistry registry)
        {
            _remoteConfig = registry.Get<IRemoteConfigService>();
            _events = registry.Get<IEventService>();

            RebuildBaseline();
            _remoteConfig.ConfigurationActivated += OnConfigurationActivated;
        }

        public void Shutdown()
        {
            _remoteConfig.ConfigurationActivated -= OnConfigurationActivated;
        }

        public bool IsEnabled(string key, bool defaultValue = false) => _remoteConfig.GetBool(key, defaultValue);

        private void OnConfigurationActivated(RemoteConfigSnapshot snapshot)
        {
            foreach (RemoteConfigDefinition definition in _remoteConfig.Definitions)
            {
                if (definition.Type != RemoteConfigValueType.Bool)
                {
                    continue;
                }

                bool newValue = _remoteConfig.GetBool(definition.Key, (bool)definition.DefaultValueBoxed);
                if (_baseline.TryGetValue(definition.Key, out bool previous) && previous == newValue)
                {
                    continue;
                }

                _baseline[definition.Key] = newValue;
                FlagChanged?.Invoke(definition.Key, newValue);
                _events.Publish(new FeatureFlagChangedEvent(definition.Key, newValue));
            }
        }

        private void RebuildBaseline()
        {
            _baseline.Clear();
            foreach (RemoteConfigDefinition definition in _remoteConfig.Definitions)
            {
                if (definition.Type == RemoteConfigValueType.Bool)
                {
                    _baseline[definition.Key] = _remoteConfig.GetBool(definition.Key, (bool)definition.DefaultValueBoxed);
                }
            }
        }
    }
}
