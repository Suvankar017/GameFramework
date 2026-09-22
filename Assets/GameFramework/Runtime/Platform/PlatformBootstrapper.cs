using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 14's platform
    /// services - the same registration-extension mechanism every other phase's bootstrapper
    /// subclass uses (compare <c>PlayerDataBootstrapper</c>, <c>PerformanceBootstrapper</c>).
    ///
    /// Sibling, not a base or subclass, of <c>PlayerSystemsBootstrapper</c>/
    /// <c>PerformanceBootstrapper</c>/<c>PlayerDataBootstrapper</c>: nothing in
    /// <see cref="GameFramework.Platform"/> references Input/UI/Audio/Feedback/Gameplay/Performance
    /// types, so it stays usable by any game regardless of which of those it also uses (see
    /// CLAUDE.md's Phase 14 brief, section 2). Application lifecycle (pause/resume/focus/quit) is
    /// deliberately not re-registered here - Phase 5's existing
    /// <c>Performance.Mobile.IApplicationLifecycleService</c> already covers exactly that, so a game
    /// using both phases gets one lifecycle relay, not two competing ones (see that interface's own
    /// remarks and this brief's section 10). A game that wants Platform without Performance still
    /// gets full pause/focus/quit visibility by registering
    /// <c>Performance.Mobile.ApplicationLifecycleService</c> directly, the same soft-integration
    /// story Phase 13's <c>PlayerDataLifecycleDriver</c> already established for itself.
    ///
    /// Registration order matters: <see cref="IDeviceInfoService"/> and <see cref="IAppStoreService"/>/
    /// <see cref="IAppSettingsService"/> resolve <see cref="IPlatformService"/> (and, for the store
    /// service, <see cref="IPlatformUrlService"/>) during their own <c>Initialize</c>, so all three
    /// must already be registered by then.
    /// </summary>
    public class PlatformBootstrapper : GameBootstrapper
    {
        [SerializeField]
        [Tooltip("Optional - store/review actions report unsupported without one.")]
        private AppStoreConfig _appStoreConfig;

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IPlatformService>(new PlatformService());
            registry.Register<IDeviceInfoService>(new DeviceInfoService());
            registry.Register<IScreenService>(new ScreenService());
            registry.Register<IClipboardService>(new ClipboardService());
            registry.Register<IPlatformUrlService>(new PlatformUrlService());
            registry.Register<IAppStoreService>(new AppStoreService(_appStoreConfig));
            registry.Register<INetworkReachabilityService>(new NetworkReachabilityService());
            registry.Register<IPermissionService>(new PermissionService());
            registry.Register<IAppSettingsService>(new AppSettingsService());
        }
    }
}
