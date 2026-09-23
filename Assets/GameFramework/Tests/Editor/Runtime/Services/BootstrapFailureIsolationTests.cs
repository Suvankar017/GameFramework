using System;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameFramework.Runtime.Tests.Services
{
    /// <summary>Phase 19 startup hardening: one service failing to initialize (or shut down) must
    /// not abort the rest of the framework. Calls the internal <c>Initialize</c> directly - Awake does
    /// not run for AddComponent in Edit Mode.</summary>
    public class BootstrapFailureIsolationTests
    {
        private interface IHealthyService : IGameService { bool Initialized { get; } bool ShutDown { get; } }
        private interface IFailingService : IGameService { int ShutdownCalls { get; } }
        private interface ISoftDependentService : IGameService { bool SawFailingService { get; } }
        private interface IHardDependentService : IGameService { }
        private interface IThrowingShutdownService : IGameService { }

        private sealed class HealthyService : IHealthyService
        {
            public bool Initialized { get; private set; }
            public bool ShutDown { get; private set; }
            public void Initialize(IServiceRegistry registry) => Initialized = true;
            public void Shutdown() => ShutDown = true;
        }

        private sealed class FailingService : IFailingService
        {
            public int ShutdownCalls { get; private set; }
            public void Initialize(IServiceRegistry registry) => throw new InvalidOperationException("Simulated provider SDK failure.");
            public void Shutdown() => ShutdownCalls++;
        }

        private sealed class SoftDependentService : ISoftDependentService
        {
            public bool SawFailingService { get; private set; }
            public void Initialize(IServiceRegistry registry) => SawFailingService = registry.TryGet(out IFailingService _);
            public void Shutdown() { }
        }

        private sealed class HardDependentService : IHardDependentService
        {
            public void Initialize(IServiceRegistry registry) => registry.Get<IFailingService>();
            public void Shutdown() { }
        }

        private sealed class ThrowingShutdownService : IThrowingShutdownService
        {
            public void Initialize(IServiceRegistry registry) { }
            public void Shutdown() => throw new InvalidOperationException("Simulated shutdown failure.");
        }

        private sealed class TestBootstrapper : GameBootstrapper
        {
            public static Action<IServiceRegistry> Registration;

            protected override void RegisterServices(IServiceRegistry registry) => Registration(registry);
        }

        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("BootstrapFailureIsolationTests");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
            TestBootstrapper.Registration = null;
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Initialize_OneServiceThrows_OthersStillInitializeAndStateIsReady()
        {
            // Set per test: the runner resets this flag after SetUp. Failures are logged as errors by design.
            LogAssert.ignoreFailingMessages = true;

            var healthy = new HealthyService();
            var failing = new FailingService();
            var soft = new SoftDependentService();
            TestBootstrapper.Registration = registry =>
            {
                registry.Register<IHealthyService>(healthy);
                registry.Register<IFailingService>(failing);
                registry.Register<ISoftDependentService>(soft);
            };

            TestBootstrapper bootstrapper = _gameObject.AddComponent<TestBootstrapper>();
            bootstrapper.Initialize();

            Assert.AreEqual(BootstrapState.Ready, bootstrapper.State);
            Assert.IsTrue(healthy.Initialized);
            Assert.IsFalse(soft.SawFailingService, "A soft dependent must see the failed service as unavailable.");
            Assert.AreEqual(1, bootstrapper.InitializationFailures.Count);
            Assert.AreEqual(typeof(IFailingService), bootstrapper.InitializationFailures[0].ServiceType);
            Assert.AreEqual(1, failing.ShutdownCalls, "A failed service gets one best-effort cleanup call.");
        }

        [Test]
        public void Initialize_HardDependencyOnFailedService_AlsoRecordedWithClearMessage()
        {
            // Set per test: the runner resets this flag after SetUp. Failures are logged as errors by design.
            LogAssert.ignoreFailingMessages = true;

            TestBootstrapper.Registration = registry =>
            {
                registry.Register<IFailingService>(new FailingService());
                registry.Register<IHardDependentService>(new HardDependentService());
            };

            TestBootstrapper bootstrapper = _gameObject.AddComponent<TestBootstrapper>();
            bootstrapper.Initialize();

            Assert.AreEqual(2, bootstrapper.InitializationFailures.Count);
            var exception = Assert.Throws<InvalidOperationException>(() => bootstrapper.Services.Get<IFailingService>());
            StringAssert.Contains("failed to initialize", exception.Message);
        }

        [Test]
        public void Shutdown_OneServiceThrows_RemainingServicesStillShutDown()
        {
            // Set per test: the runner resets this flag after SetUp. Failures are logged as errors by design.
            LogAssert.ignoreFailingMessages = true;

            var healthy = new HealthyService();
            TestBootstrapper.Registration = registry =>
            {
                registry.Register<IHealthyService>(healthy);
                registry.Register<IThrowingShutdownService>(new ThrowingShutdownService());
            };

            TestBootstrapper bootstrapper = _gameObject.AddComponent<TestBootstrapper>();
            bootstrapper.Initialize();
            bootstrapper.Shutdown();

            Assert.IsTrue(healthy.ShutDown, "Reverse-order shutdown must continue past a throwing service.");
            Assert.AreEqual(BootstrapState.Shutdown, bootstrapper.State);
        }

        [Test]
        public void Shutdown_FailedService_IsNotShutDownAgain()
        {
            // Set per test: the runner resets this flag after SetUp. Failures are logged as errors by design.
            LogAssert.ignoreFailingMessages = true;

            var failing = new FailingService();
            TestBootstrapper.Registration = registry => registry.Register<IFailingService>(failing);

            TestBootstrapper bootstrapper = _gameObject.AddComponent<TestBootstrapper>();
            bootstrapper.Initialize();
            bootstrapper.Shutdown();

            Assert.AreEqual(1, failing.ShutdownCalls);
        }
    }
}
