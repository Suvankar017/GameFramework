namespace GameFramework.RemoteConfig.Providers.Mock
{
    /// <summary>See CLAUDE.md's Phase 17 brief, sections 22/67-68.</summary>
    public enum MockRemoteConfigSimulationMode
    {
        /// <summary>Serves the authored <see cref="MockRemoteConfigEntry"/> values.</summary>
        AlwaysSucceed,

        /// <summary>Reports failure immediately - simulates no network/backend unavailable.</summary>
        AlwaysUnavailable,

        /// <summary>Never calls back - simulates a hung network call, exercising
        /// <see cref="RemoteConfigService"/>'s own fetch-timeout path.</summary>
        AlwaysTimeout,

        /// <summary>Reports a schema version one higher than requested, exercising the "unsupported
        /// schema" rejection path.</summary>
        SchemaMismatch
    }
}
