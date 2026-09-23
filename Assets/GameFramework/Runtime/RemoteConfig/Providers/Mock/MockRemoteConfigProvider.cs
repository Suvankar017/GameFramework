using System;
using System.Collections.Generic;

namespace GameFramework.RemoteConfig.Providers.Mock
{
    /// <summary>
    /// Deterministic <see cref="IRemoteConfigProvider"/> for the Editor and local development - see
    /// CLAUDE.md's Phase 17 brief, sections 22/67-68. This is the framework's only shipped provider in
    /// this phase; no Firebase/Unity Remote Config/PlayFab adapter exists because none of those SDKs
    /// is installed in this project (see <see cref="IRemoteConfigProvider"/>'s remarks). Distinct from
    /// the <c>Tests</c> assembly's own fully-controllable fake (used for edge cases like invalid data
    /// or an out-of-range value) - this one is the shipped, game-usable "I have no backend yet but
    /// want to see fetch/activation flow" provider, wired up by <see cref="RemoteConfigBootstrapper"/>'s
    /// inspector toggle.
    /// </summary>
    public sealed class MockRemoteConfigProvider : IRemoteConfigProvider
    {
        private readonly MockRemoteConfigSimulationMode _mode;
        private readonly IReadOnlyList<MockRemoteConfigEntry> _entries;
        private int _fetchCounter;

        public MockRemoteConfigProvider(MockRemoteConfigSimulationMode mode, IReadOnlyList<MockRemoteConfigEntry> entries)
        {
            _mode = mode;
            _entries = entries ?? Array.Empty<MockRemoteConfigEntry>();
        }

        public void Initialize(Action<bool> onComplete) => onComplete?.Invoke(true);

        public void Fetch(int currentSchemaVersion, Action<RemoteConfigProviderResult> onComplete)
        {
            switch (_mode)
            {
                case MockRemoteConfigSimulationMode.AlwaysUnavailable:
                    onComplete?.Invoke(RemoteConfigProviderResult.Failed("Mock: provider unavailable."));
                    return;

                case MockRemoteConfigSimulationMode.AlwaysTimeout:
                    // Deliberately never calls onComplete.
                    return;
            }

            _fetchCounter++;
            var values = new Dictionary<string, object>(_entries.Count, StringComparer.Ordinal);
            for (int i = 0; i < _entries.Count; i++)
            {
                MockRemoteConfigEntry entry = _entries[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    values[entry.Key] = entry.BoxedValue;
                }
            }

            int schemaVersion = _mode == MockRemoteConfigSimulationMode.SchemaMismatch
                ? currentSchemaVersion + 1
                : currentSchemaVersion;

            onComplete?.Invoke(RemoteConfigProviderResult.Successful(_fetchCounter, schemaVersion, values, DateTime.UtcNow));
        }
    }
}
