using System.Collections.Generic;
using GameFramework.RemoteConfig.FeatureFlags;
using GameFramework.RemoteConfig.Providers;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.RemoteConfig.Tests
{
    public class FeatureFlagServiceTests
    {
        private ServiceRegistry _registry;
        private EventService _events;
        private RemoteConfigConfiguration _configuration;
        private FakeRemoteConfigProvider _provider;
        private RemoteConfigService _remoteConfig;
        private FeatureFlagService _flags;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _events, out PersistenceService _, out TimerService _);

            _configuration = TestDefinitions.Configuration(new[]
            {
                TestDefinitions.BoolDefinition("features.new_shop", false),
            });

            _provider = new FakeRemoteConfigProvider();
            _remoteConfig = new RemoteConfigService(_configuration, _provider);
            _remoteConfig.Initialize(_registry);
            _registry.Register<IRemoteConfigService>(_remoteConfig);
            _registry.MarkInitialized(typeof(IRemoteConfigService));

            _flags = new FeatureFlagService();
            _flags.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _flags.Shutdown();
            _remoteConfig.Shutdown();
            Object.DestroyImmediate(_configuration);
        }

        [Test]
        public void IsEnabled_NoOverride_ReturnsDeclaredDefault()
        {
            Assert.IsFalse(_flags.IsEnabled("features.new_shop"));
        }

        [Test]
        public void IsEnabled_UndeclaredKey_ReturnsCallerDefault()
        {
            Assert.IsTrue(_flags.IsEnabled("features.undeclared", true));
        }

        [Test]
        public void RemoteOverride_TrueToFalseTransition_RaisesFlagChangedOnce()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object> { ["features.new_shop"] = true }));

            var changes = new List<(string Key, bool Value)>();
            _flags.FlagChanged += (key, value) => changes.Add((key, value));

            bool eventPublished = false;
            _events.Subscribe<FeatureFlagChangedEvent>(_ => eventPublished = true);

            _remoteConfig.Fetch();

            Assert.IsTrue(_flags.IsEnabled("features.new_shop"));
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual("features.new_shop", changes[0].Key);
            Assert.IsTrue(changes[0].Value);
            Assert.IsTrue(eventPublished);
        }

        [Test]
        public void RemoteFetch_SameValueAsBefore_DoesNotRaiseFlagChanged()
        {
            _provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object> { ["features.new_shop"] = false }));

            int changeCount = 0;
            _flags.FlagChanged += (key, value) => changeCount++;

            _remoteConfig.Fetch();

            Assert.AreEqual(0, changeCount);
        }
    }
}
