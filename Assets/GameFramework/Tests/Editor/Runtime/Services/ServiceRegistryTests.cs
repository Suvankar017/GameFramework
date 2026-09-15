using System;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Services
{
    public class ServiceRegistryTests
    {
        private interface IFakeServiceA
        {
        }

        private interface IFakeServiceB
        {
        }

        private class FakeService : IFakeServiceA, IFakeServiceB, IGameService
        {
            public int InitializeCallCount;
            public int ShutdownCallCount;
            public IServiceRegistry RegistryPassedToInitialize;

            public void Initialize(IServiceRegistry registry)
            {
                InitializeCallCount++;
                RegistryPassedToInitialize = registry;
            }

            public void Shutdown()
            {
                ShutdownCallCount++;
            }
        }

        private ServiceRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new ServiceRegistry();
        }

        [Test]
        public void Register_ThenMarkInitialized_Get_ReturnsSameInstance()
        {
            var service = new FakeService();

            _registry.Register<IFakeServiceA>(service);
            _registry.MarkInitialized(typeof(IFakeServiceA));

            Assert.AreSame(service, _registry.Get<IFakeServiceA>());
        }

        [Test]
        public void Register_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _registry.Register<IFakeServiceA>(null));
        }

        [Test]
        public void Register_DuplicateType_ThrowsInvalidOperationException()
        {
            _registry.Register<IFakeServiceA>(new FakeService());

            Assert.Throws<InvalidOperationException>(() => _registry.Register<IFakeServiceA>(new FakeService()));
        }

        [Test]
        public void Get_UnregisteredType_ThrowsServiceNotFoundException()
        {
            Assert.Throws<ServiceNotFoundException>(() => _registry.Get<IFakeServiceA>());
        }

        [Test]
        public void Get_RegisteredButNotInitialized_ThrowsInvalidOperationException()
        {
            _registry.Register<IFakeServiceA>(new FakeService());

            Assert.Throws<InvalidOperationException>(() => _registry.Get<IFakeServiceA>());
        }

        [Test]
        public void TryGet_RegisteredButNotInitialized_ReturnsFalse()
        {
            _registry.Register<IFakeServiceA>(new FakeService());

            bool found = _registry.TryGet(out IFakeServiceA service);

            Assert.IsFalse(found);
            Assert.IsNull(service);
        }

        [Test]
        public void TryGet_Initialized_ReturnsTrueAndInstance()
        {
            var fake = new FakeService();
            _registry.Register<IFakeServiceA>(fake);
            _registry.MarkInitialized(typeof(IFakeServiceA));

            bool found = _registry.TryGet(out IFakeServiceA service);

            Assert.IsTrue(found);
            Assert.AreSame(fake, service);
        }

        [Test]
        public void TryGet_Unregistered_ReturnsFalse()
        {
            bool found = _registry.TryGet(out IFakeServiceA service);

            Assert.IsFalse(found);
            Assert.IsNull(service);
        }

        [Test]
        public void IsRegistered_ReflectsRegistrationRegardlessOfInitializationState()
        {
            Assert.IsFalse(_registry.IsRegistered<IFakeServiceA>());

            _registry.Register<IFakeServiceA>(new FakeService());

            Assert.IsTrue(_registry.IsRegistered<IFakeServiceA>());
        }

        [Test]
        public void RegistrationOrder_ReflectsRegistrationSequence()
        {
            _registry.Register<IFakeServiceA>(new FakeService());
            _registry.Register<IFakeServiceB>(new FakeService());

            CollectionAssert.AreEqual(
                new[] { typeof(IFakeServiceA), typeof(IFakeServiceB) },
                _registry.RegistrationOrder);
        }

        [Test]
        public void Clear_RemovesAllEntriesAndRegistrationOrder()
        {
            _registry.Register<IFakeServiceA>(new FakeService());

            _registry.Clear();

            Assert.IsFalse(_registry.IsRegistered<IFakeServiceA>());
            Assert.AreEqual(0, _registry.RegistrationOrder.Count);
        }

        [Test]
        public void EarlierRegisteredService_CanBeLookedUpDuringLaterServicesInitialize()
        {
            var dependency = new FakeService();
            var dependent = new DependentFakeService();

            _registry.Register<IFakeServiceA>(dependency);
            _registry.Register<IFakeServiceB>(dependent);

            foreach (Type type in _registry.RegistrationOrder)
            {
                ((IGameService)_registry.GetInstance(type)).Initialize(_registry);
                _registry.MarkInitialized(type);
            }

            Assert.AreSame(dependency, dependent.ResolvedDependency);
        }

        private class DependentFakeService : IFakeServiceB, IGameService
        {
            public IFakeServiceA ResolvedDependency;

            public void Initialize(IServiceRegistry registry)
            {
                ResolvedDependency = registry.Get<IFakeServiceA>();
            }

            public void Shutdown()
            {
            }
        }
    }
}
