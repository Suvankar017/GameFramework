using GameFramework.Analytics.Diagnostics;
using GameFramework.Analytics.Diagnostics.Mock;
using GameFramework.Analytics.Providers;
using GameFramework.Analytics.Providers.Mock;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Security;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Analytics
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 16's two services
    /// (<see cref="IDiagnosticsService"/>, <see cref="IAnalyticsService"/>) - the same registration-
    /// extension mechanism every other phase's bootstrapper subclass uses (compare
    /// <c>PlayerDataBootstrapper</c>, <c>PlatformBootstrapper</c>).
    ///
    /// A <see cref="GameBootstrapper"/> subclass, not a <see cref="PlayerSystemsBootstrapper"/> or
    /// <c>ProgressionBootstrapper</c> subclass: neither service has a hard dependency on Input/UI/
    /// Rewards - every dependency <see cref="AnalyticsService"/>/<see cref="DiagnosticsService"/>
    /// actually require (<c>IEventService</c>, <c>IPersistenceService</c>) is already part of the base
    /// eight services <see cref="GameBootstrapper"/> itself registers; <c>Platform</c>/<c>Performance</c>
    /// integration is resolved softly at runtime (see those services' remarks).
    ///
    /// Registers <see cref="IDiagnosticsService"/> before <see cref="IAnalyticsService"/> - the latter
    /// resolves the former softly (<c>registry.TryGet</c>) during its own <c>Initialize</c>, which
    /// only succeeds once it is already registered and initialized.
    ///
    /// Defaults to <see cref="NoOpAnalyticsProvider"/>/<see cref="NoOpCrashReportingProvider"/> - see
    /// CLAUDE.md's Phase 16 brief, section 54 ("production builds must not accidentally use
    /// MockAnalyticsProvider"). The two inspector toggles below opt into the shipped Mock providers
    /// for local development/testing only; a game shipping with a real analytics/crash SDK replaces
    /// these two constructor arguments with its own <see cref="IAnalyticsProvider"/>/
    /// <see cref="ICrashReportingProvider"/> implementation once a provider SDK is installed - nothing
    /// else in this bootstrapper, or in <see cref="AnalyticsService"/>/<see cref="DiagnosticsService"/>,
    /// changes (see CLAUDE.md's Phase 16 brief, section 45).
    /// </summary>
    public class AnalyticsBootstrapper : GameBootstrapper
    {
        [Header("Analytics Content")]
        [SerializeField] private AnalyticsConfiguration _analyticsConfiguration;
        [SerializeField] private DiagnosticsConfiguration _diagnosticsConfiguration;

        [Header("Mock Providers (Editor/testing only - no analytics/crash SDK installed, see class remarks)")]
        [SerializeField] private bool _useMockAnalyticsProvider;
        [SerializeField] private bool _useMockCrashReportingProvider;

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            // Phase 19: the mock toggles are honored only in development builds - see DevelopmentProviderGuard.
            IAnalyticsProvider analyticsProvider = DevelopmentProviderGuard.Select<IAnalyticsProvider>(
                _useMockAnalyticsProvider, () => new MockAnalyticsProvider(), () => new NoOpAnalyticsProvider(), nameof(AnalyticsBootstrapper));

            ICrashReportingProvider crashProvider = DevelopmentProviderGuard.Select<ICrashReportingProvider>(
                _useMockCrashReportingProvider, () => new MockCrashReportingProvider(), () => new NoOpCrashReportingProvider(), nameof(AnalyticsBootstrapper));

            registry.Register<IDiagnosticsService>(new DiagnosticsService(_diagnosticsConfiguration, crashProvider));
            registry.Register<IAnalyticsService>(new AnalyticsService(_analyticsConfiguration, analyticsProvider));
        }
    }
}
