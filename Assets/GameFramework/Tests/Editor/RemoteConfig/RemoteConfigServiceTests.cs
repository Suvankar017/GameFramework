using System.Collections.Generic;
using GameFramework.RemoteConfig.Providers;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.RemoteConfig.Tests
{
    public class RemoteConfigServiceTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private TimerService _timer;
        private FakeTimeService _time;
        private RemoteConfigConfiguration _configuration;
        private FakeRemoteConfigProvider _provider;
        private RemoteConfigService _service;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _events, out _persistence, out _timer);
            _time = (FakeTimeService)_registry.Get<GameFramework.Runtime.Time.ITimeService>();

            _configuration = TestDefinitions.Configuration(new[]
            {
                TestDefinitions.IntDefinition("economy.reward_multiplier", 1, hasRange: true, min: 1, max: 10),
                TestDefinitions.BoolDefinition("features.new_shop", false),
                TestDefinitions.StringDefinition("ui.banner_text", "default"),
            }, fetchTimeoutSeconds: 5f);

            _provider = new FakeRemoteConfigProvider();
            _service = new RemoteConfigService(_configuration, _provider);
            _service.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Shutdown();
            Object.DestroyImmediate(_configuration);
        }

        [Test]
        public void Initialize_UsesDeclaredDefaults()
        {
            Assert.AreEqual(RemoteConfigState.Ready, _service.State);
            Assert.AreEqual(1, _service.GetInt("economy.reward_multiplier"));
            Assert.IsFalse(_service.GetBool("features.new_shop"));
            Assert.AreEqual("default", _service.GetString("ui.banner_text"));
        }

        [Test]
        public void GetInt_UndeclaredKey_ReturnsCallerSuppliedDefault()
        {
            Assert.AreEqual(42, _service.GetInt("gameplay.undeclared_key", 42));
        }

        [Test]
        public void Fetch_Success_ActivatesSnapshotAndPublishesEvents()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object>
            {
                ["economy.reward_multiplier"] = 5,
            }));

            bool activatedEventPublished = false;
            _events.Subscribe<ConfigActivatedEvent>(_ => activatedEventPublished = true);

            RemoteConfigFetchResult? result = null;
            _service.Fetch(r => result = r);

            Assert.IsTrue(result.Value.Success);
            Assert.AreEqual(RemoteConfigState.Active, _service.State);
            Assert.AreEqual(5, _service.GetInt("economy.reward_multiplier"));
            Assert.IsTrue(activatedEventPublished);
        }

        [Test]
        public void Fetch_InvalidType_RejectsEntireSnapshot()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object>
            {
                ["economy.reward_multiplier"] = 5,
                ["features.new_shop"] = "not-a-bool",
            }));

            RemoteConfigFetchResult? result = null;
            _service.Fetch(r => result = r);

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, result.Value.Kind);
            // Atomicity: the valid key in the same payload must NOT have been applied either.
            Assert.AreEqual(1, _service.GetInt("economy.reward_multiplier"));
            Assert.AreEqual(RemoteConfigState.Ready, _service.State);
        }

        [Test]
        public void Fetch_OutOfRange_Rejected()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object>
            {
                ["economy.reward_multiplier"] = 999,
            }));

            RemoteConfigFetchResult? result = null;
            _service.Fetch(r => result = r);

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, result.Value.Kind);
            Assert.AreEqual(1, _service.GetInt("economy.reward_multiplier"));
        }

        [Test]
        public void Fetch_SchemaNewerThanSupported_Rejected()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 2, new Dictionary<string, object>()));

            RemoteConfigFetchResult? result = null;
            _service.Fetch(r => result = r);

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, result.Value.Kind);
        }

        [Test]
        public void Fetch_ProviderUnavailable_KeepsExistingSnapshot()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Failed("Mock: unavailable."));

            RemoteConfigFetchResult? result = null;
            _service.Fetch(r => result = r);

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, result.Value.Kind);
            Assert.AreEqual(RemoteConfigState.Ready, _service.State);
            Assert.AreEqual(1, _service.GetInt("economy.reward_multiplier"));
        }

        [Test]
        public void Fetch_AlreadyInProgress_ReturnsAlreadyInProgress()
        {
            _provider.SuppressCallback = true;
            _service.Fetch();

            RemoteConfigFetchResult? second = null;
            _service.Fetch(r => second = r);

            Assert.AreEqual(RemoteConfigFetchResultKind.AlreadyInProgress, second.Value.Kind);
        }

        [Test]
        public void Fetch_LateProviderCallbackAfterTimeout_IsIgnored()
        {
            _provider.SuppressCallback = true;

            RemoteConfigFetchResult? result = null;
            _service.Fetch(r => result = r);

            _time.UnscaledDeltaTime = 10f; // exceeds the 5s FetchTimeoutSeconds configured in SetUp
            _timer.Tick();

            Assert.AreEqual(RemoteConfigFetchResultKind.TimedOut, result.Value.Kind);

            // The provider "responds" after the timeout already resolved the fetch - must not
            // resurrect/override the timed-out outcome.
            _provider.CompleteSuppressedFetch(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object> { ["economy.reward_multiplier"] = 7 }));

            Assert.AreEqual(1, _service.GetInt("economy.reward_multiplier"));
        }

        [Test]
        public void Fetch_SecondFetchOmittingAKey_RevertsThatKeyToDefault()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object> { ["economy.reward_multiplier"] = 5 }));
            _service.Fetch();
            Assert.AreEqual(5, _service.GetInt("economy.reward_multiplier"));

            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(2, 1, new Dictionary<string, object>()));
            _service.Fetch();

            Assert.AreEqual(1, _service.GetInt("economy.reward_multiplier"));
        }
    }
}
