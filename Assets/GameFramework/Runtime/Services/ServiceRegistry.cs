using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;

namespace GameFramework.Runtime.Services
{
    /// <summary>
    /// Type-keyed service container. Lookup is a dictionary hit keyed by <see cref="Type"/> — no
    /// reflection scanning, no scene search.
    ///
    /// Initialization is two-phase and owned by the bootstrap that constructs this registry, not
    /// by the registry itself: every service is registered first, then <see cref="MarkInitialized"/>
    /// is called for each as its own <see cref="IGameService.Initialize"/> completes.
    /// <see cref="Get{TService}"/> only succeeds once a service has been marked initialized, so a
    /// service can safely look up another service that was registered and initialized earlier in
    /// the same sequence from inside its own Initialize — and a lookup that happens too early, or
    /// for a service that doesn't exist, fails immediately instead of silently returning a
    /// half-constructed instance.
    ///
    /// The orchestration members (<see cref="RegistrationOrder"/>, <see cref="GetInstance"/>,
    /// <see cref="MarkInitialized"/>, <see cref="Clear"/>) are internal — only the owning
    /// bootstrap in this assembly uses them. Everything else sees only <see cref="IServiceRegistry"/>.
    /// </summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private sealed class Entry
        {
            public object Instance;
            public bool IsInitialized;
        }

        private readonly Dictionary<Type, Entry> _entries = new Dictionary<Type, Entry>();
        private readonly List<Type> _registrationOrder = new List<Type>();

        public void Register<TService>(TService service) where TService : class
        {
            Guard.NotNull(service, nameof(service));

            Type type = typeof(TService);
            if (_entries.ContainsKey(type))
            {
                throw new InvalidOperationException($"A service of type '{type.Name}' is already registered.");
            }

            _entries.Add(type, new Entry { Instance = service, IsInitialized = false });
            _registrationOrder.Add(type);
        }

        public TService Get<TService>() where TService : class
        {
            Type type = typeof(TService);
            if (!_entries.TryGetValue(type, out Entry entry))
            {
                throw new ServiceNotFoundException(type);
            }

            if (!entry.IsInitialized)
            {
                throw new InvalidOperationException(
                    $"Service '{type.Name}' has been registered but is not yet initialized.");
            }

            return (TService)entry.Instance;
        }

        public bool TryGet<TService>(out TService service) where TService : class
        {
            Type type = typeof(TService);
            if (_entries.TryGetValue(type, out Entry entry) && entry.IsInitialized)
            {
                service = (TService)entry.Instance;
                return true;
            }

            service = null;
            return false;
        }

        public bool IsRegistered<TService>() where TService : class
        {
            return _entries.ContainsKey(typeof(TService));
        }

        internal IReadOnlyList<Type> RegistrationOrder => _registrationOrder;

        internal object GetInstance(Type serviceType) => _entries[serviceType].Instance;

        internal void MarkInitialized(Type serviceType) => _entries[serviceType].IsInitialized = true;

        internal void Clear()
        {
            _entries.Clear();
            _registrationOrder.Clear();
        }
    }
}
