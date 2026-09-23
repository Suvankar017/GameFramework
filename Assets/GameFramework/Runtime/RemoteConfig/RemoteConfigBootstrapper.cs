using GameFramework.RemoteConfig.FeatureFlags;
using GameFramework.RemoteConfig.LiveOps;
using GameFramework.RemoteConfig.Providers;
using GameFramework.RemoteConfig.Providers.Mock;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 17's three services
    /// (<see cref="IRemoteConfigService"/>, <see cref="IFeatureFlagService"/>,
    /// <see cref="ILiveOpsService"/>) - the same registration-extension mechanism every other phase's
    /// bootstrapper subclass uses (compare <c>AnalyticsBootstrapper</c>, <c>PlatformBootstrapper</c>).
    ///
    /// A <see cref="GameBootstrapper"/> subclass, not a <c>PlayerSystemsBootstrapper</c>/
    /// <c>ProgressionBootstrapper</c> subclass: none of these three services has a hard dependency on
    /// Input/UI/Rewards - every dependency <see cref="RemoteConfigService"/> actually requires
    /// (<c>IPersistenceService</c>, <c>IEventService</c>, <c>ITimerService</c>) is already part of the
    /// base eight services <see cref="GameBootstrapper"/> itself registers.
    ///
    /// Registration order is load-bearing: <see cref="IRemoteConfigService"/> first, since
    /// <see cref="IFeatureFlagService"/>/<see cref="ILiveOpsService"/> both resolve it via
    /// <c>registry.Get</c> during their own <c>Initialize</c>, which only succeeds once it is already
    /// registered and initialized.
    ///
    /// Defaults to <see cref="NoOpRemoteConfigProvider"/> - see CLAUDE.md's Phase 17 brief, section 23:
    /// no Firebase Remote Config/Unity Remote Config/PlayFab SDK is installed in this project. The
    /// inspector toggle below opts into the shipped <see cref="MockRemoteConfigProvider"/> for local
    /// development/testing only; a game shipping with a real remote config backend replaces the
    /// constructor argument with its own <see cref="IRemoteConfigProvider"/> implementation once a
    /// provider SDK is installed - nothing else in this bootstrapper, or in
    /// <see cref="RemoteConfigService"/>, changes.
    /// </summary>
    public class RemoteConfigBootstrapper : GameBootstrapper
    {
        [Header("Remote Config Content")]
        [SerializeField] private RemoteConfigConfiguration _remoteConfigConfiguration;
        [SerializeField] private LiveOpsConfiguration _liveOpsConfiguration;

        [Header("Mock Provider (Editor/testing only - no remote config SDK installed, see class remarks)")]
        [SerializeField] private bool _useMockProvider;
        [SerializeField] private MockRemoteConfigSimulationMode _mockSimulationMode = MockRemoteConfigSimulationMode.AlwaysSucceed;
        [SerializeField] private MockRemoteConfigEntry[] _mockEntries = System.Array.Empty<MockRemoteConfigEntry>();

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            IRemoteConfigProvider provider = _useMockProvider
                ? (IRemoteConfigProvider)new MockRemoteConfigProvider(_mockSimulationMode, _mockEntries)
                : new NoOpRemoteConfigProvider();

            registry.Register<IRemoteConfigService>(new RemoteConfigService(_remoteConfigConfiguration, provider));
            registry.Register<IFeatureFlagService>(new FeatureFlagService());
            registry.Register<ILiveOpsService>(new LiveOpsService(_liveOpsConfiguration));
        }
    }
}
