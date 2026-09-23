using GameFramework.Notifications.Providers;
using GameFramework.Notifications.Providers.Mock;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 18's
    /// <see cref="INotificationService"/> - the same registration-extension mechanism every other
    /// phase's bootstrapper subclass uses (compare <c>AnalyticsBootstrapper</c>,
    /// <c>RemoteConfigBootstrapper</c>).
    ///
    /// A bare <see cref="GameBootstrapper"/> subclass, not a <c>PlayerSystemsBootstrapper</c>/
    /// <c>ProgressionBootstrapper</c> subclass: <see cref="NotificationService"/> has no hard
    /// dependency on Input/UI/Rewards - every dependency it actually requires (<c>IEventService</c>)
    /// is already part of the base eight services <see cref="GameBootstrapper"/> registers;
    /// <c>Localization.ILocalizationService</c> is resolved softly at runtime (see
    /// <see cref="NotificationService"/>'s remarks) - register it *before* this bootstrapper's
    /// services in a game's own combined subclass if localized notification content is wanted.
    ///
    /// Defaults to <see cref="NoOpNotificationProvider"/> - see CLAUDE.md's Phase 18 brief, section
    /// 2/39: no notification SDK (Unity Mobile Notifications/Firebase/OneSignal) is installed in this
    /// project. The inspector toggle below opts into the shipped <see cref="MockNotificationProvider"/>
    /// for local development/testing only; a game shipping with a real notification SDK replaces the
    /// constructor argument with its own <see cref="INotificationProvider"/> implementation once one
    /// is installed - nothing else in this bootstrapper, or in <see cref="NotificationService"/>,
    /// changes.
    /// </summary>
    public class NotificationsBootstrapper : GameBootstrapper
    {
        [Header("Mock Provider (Editor/testing only - no notification SDK installed, see class remarks)")]
        [SerializeField] private bool _useMockProvider;
        [SerializeField] private MockNotificationSimulationMode _mockSimulationMode = MockNotificationSimulationMode.AlwaysSucceed;

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            INotificationProvider provider = _useMockProvider
                ? (INotificationProvider)new MockNotificationProvider(_mockSimulationMode)
                : new NoOpNotificationProvider();

            registry.Register<INotificationService>(new NotificationService(provider));
        }
    }
}
