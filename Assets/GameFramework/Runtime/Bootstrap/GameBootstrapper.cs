using System;
using System.Collections.Generic;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.State;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using UnityEngine;

namespace GameFramework.Runtime.Bootstrap
{
    /// <summary>
    /// Framework entry point. Place one instance on a GameObject in a startup scene (e.g. a
    /// dedicated bootstrap scene loaded first, or additively before the first game scene). On
    /// Awake it claims a singleton, survives scene loads via DontDestroyOnLoad, registers the
    /// framework's core services, and initializes them in order.
    ///
    /// A second instance in any scene destroys itself in Awake rather than replacing the first —
    /// this is deliberately not "latest wins" so a duplicate is loud (a warning) rather than
    /// silently swapping out a framework already in use by other objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrapper : MonoBehaviour
    {
        public static GameBootstrapper Instance { get; private set; }

        public BootstrapState State { get; private set; } = BootstrapState.Created;

        /// <summary>Only safe to use once <see cref="State"/> is <see cref="BootstrapState.Ready"/>.</summary>
        public IServiceRegistry Services => _registry;

        public event Action<BootstrapState> StateChanged;

        /// <summary>
        /// Services whose <see cref="IGameService.Initialize"/> threw during startup. Empty in a
        /// healthy start. A failed service stays registered but is never marked initialized, so
        /// <see cref="IServiceRegistry.TryGet{TService}"/> returns false for it (soft dependents
        /// degrade) and <see cref="IServiceRegistry.Get{TService}"/> throws a message naming the real
        /// cause (hard dependents fail too, and are recorded here in turn). Startup still reaches
        /// <see cref="BootstrapState.Ready"/> - one optional provider failing must not take the whole
        /// framework down - so a game that cannot run without a specific service checks this list (or
        /// <c>TryGet</c>) and decides how to present the failure itself.
        /// </summary>
        public IReadOnlyList<ServiceInitializationFailure> InitializationFailures => _initializationFailures;

        private readonly ServiceRegistry _registry = new ServiceRegistry();
        private readonly List<IUpdatableService> _updatableServices = new List<IUpdatableService>();
        private readonly List<ServiceInitializationFailure> _initializationFailures = new List<ServiceInitializationFailure>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[Bootstrap] Duplicate {nameof(GameBootstrapper)} on '{gameObject.name}' was destroyed; " +
                    $"'{Instance.gameObject.name}' remains the active instance.",
                    this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            if (State == BootstrapState.Ready)
            {
                Shutdown();
            }

            Instance = null;
        }

        private void OnApplicationQuit()
        {
            if (State == BootstrapState.Ready)
            {
                Shutdown();
            }
        }

        private void Update()
        {
            if (State != BootstrapState.Ready)
            {
                return;
            }

            for (int i = 0; i < _updatableServices.Count; i++)
            {
                _updatableServices[i].Tick();
            }
        }

        /// <summary>
        /// Shuts down every registered service in reverse registration order and clears the
        /// registry. Safe to call more than once, or when the bootstrap never reached
        /// <see cref="BootstrapState.Ready"/> — both are treated as a no-op rather than an error,
        /// since shutdown commonly runs from application-quit/teardown paths where being strict
        /// would just add noise.
        /// </summary>
        public void Shutdown()
        {
            if (State != BootstrapState.Ready)
            {
                return;
            }

            SetState(BootstrapState.ShuttingDown);

            IReadOnlyList<Type> order = _registry.RegistrationOrder;
            for (int i = order.Count - 1; i >= 0; i--)
            {
                if (!_registry.IsInitialized(order[i]))
                {
                    continue;
                }

                try
                {
                    ((IGameService)_registry.GetInstance(order[i])).Shutdown();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Bootstrap] Service '{order[i].Name}' threw during Shutdown; continuing with the remaining services.");
                    Debug.LogException(exception);
                }
            }

            _registry.Clear();
            _updatableServices.Clear();
            _initializationFailures.Clear();

            SetState(BootstrapState.Shutdown);
        }

        // Internal (rather than private) so tests can exercise the "already initialized" guard
        // directly; Awake is the only caller in normal use and Unity only invokes it once.
        internal void Initialize()
        {
            if (State != BootstrapState.Created)
            {
                throw new InvalidOperationException($"{nameof(GameBootstrapper)} has already been initialized.");
            }

            SetState(BootstrapState.Initializing);

            RegisterServices(_registry);

            foreach (Type serviceType in _registry.RegistrationOrder)
            {
                var service = (IGameService)_registry.GetInstance(serviceType);
                try
                {
                    service.Initialize(_registry);
                }
                catch (Exception exception)
                {
                    HandleInitializationFailure(serviceType, service, exception);
                    continue;
                }

                _registry.MarkInitialized(serviceType);

                if (service is IUpdatableService updatable)
                {
                    _updatableServices.Add(updatable);
                }
            }

            SetState(BootstrapState.Ready);
        }

        /// <summary>
        /// Registers the services exposed through <see cref="Services"/>, in the order they will
        /// be initialized (and shut down in reverse). Override to add game-specific services;
        /// call <c>base.RegisterServices(registry)</c> first to keep the framework's own services
        /// available to them.
        ///
        /// Order here is load-bearing, not cosmetic: <see cref="ITimerService"/> requires
        /// <see cref="ITimeService"/> to already be initialized, and <see cref="ISettingsService"/>
        /// requires both <see cref="IPersistenceService"/> and <see cref="IEventService"/> to be —
        /// each is registered only after what it depends on.
        /// </summary>
        protected virtual void RegisterServices(IServiceRegistry registry)
        {
            registry.Register<ILoggingService>(new LoggingService());
            registry.Register<IGameStateService>(new GameStateService());
            registry.Register<ISceneService>(new SceneService());

            registry.Register<ITimeService>(new TimeService());
            registry.Register<ITimerService>(new TimerService());
            registry.Register<IEventService>(new EventService());
            registry.Register<IPersistenceService>(
                new PersistenceService(new FilePersistenceStorage(), new JsonPersistenceSerializer()));
            registry.Register<ISettingsService>(new SettingsService());
        }

        private void HandleInitializationFailure(Type serviceType, IGameService service, Exception exception)
        {
            _registry.MarkInitializationFailed(serviceType);
            _initializationFailures.Add(new ServiceInitializationFailure(serviceType, exception));

            // Debug directly, not the Log facade: the failing service may be LoggingService itself.
            Debug.LogError(
                $"[Bootstrap] Service '{serviceType.Name}' failed to initialize and is unavailable. " +
                "Services that resolve it softly will run without it; services that require it will also fail.");
            Debug.LogException(exception);

            // Give a half-initialized service the chance to release what it already created (driver
            // GameObjects, event subscriptions) - it will never be shut down by Shutdown() otherwise.
            try
            {
                service.Shutdown();
            }
            catch (Exception cleanupException)
            {
                Debug.LogWarning($"[Bootstrap] Cleanup of failed service '{serviceType.Name}' also threw: {cleanupException.GetType().Name}: {cleanupException.Message}");
            }
        }

        private void SetState(BootstrapState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
