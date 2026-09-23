using System;
using System.Collections.Generic;
using GameFramework.RemoteConfig.Providers;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.RemoteConfig.Tests
{
    /// <summary>Covers CLAUDE.md's Phase 17 brief, section 71 (fresh/stale/expired/corrupt/missing
    /// cache) by simulating an application restart: a second <see cref="RemoteConfigService"/>
    /// instance, built against a fresh registry that shares the first instance's backing
    /// <see cref="InMemoryPersistenceStorage"/> - see <c>TestRegistryFactory</c>'s remarks.</summary>
    public class CacheTests
    {
        private const string CacheKey = "GameFramework.RemoteConfig.Cache";

        private InMemoryPersistenceStorage _storage;
        private RemoteConfigConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
            _configuration = TestDefinitions.Configuration(
                new[] { TestDefinitions.IntDefinition("economy.reward_multiplier", 1) },
                staleThresholdSeconds: 3600f,
                cacheExpirationSeconds: 604800f,
                useStaleCache: true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_configuration);
        }

        private RemoteConfigService BuildAndFetch(int fetchedValue)
        {
            ServiceRegistry registry = TestRegistryFactory.Build(_storage, out EventService _, out PersistenceService _, out TimerService _);
            var provider = new FakeRemoteConfigProvider();
            provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object> { ["economy.reward_multiplier"] = fetchedValue }));

            var service = new RemoteConfigService(_configuration, provider);
            service.Initialize(registry);
            service.Fetch();
            return service;
        }

        private RemoteConfigService BuildFresh()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(_storage, out EventService _, out PersistenceService _, out TimerService _);
            var service = new RemoteConfigService(_configuration, new FakeRemoteConfigProvider());
            service.Initialize(registry);
            return service;
        }

        private void AgeCacheBy(TimeSpan age)
        {
            var persistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            RemoteConfigCacheData data = persistence.Load<RemoteConfigCacheData>(CacheKey, 1, null);
            Assert.IsNotNull(data, "Expected a cache entry to already exist.");
            data.FetchedAtUtcTicks = (DateTime.UtcNow - age).Ticks;
            persistence.Save(CacheKey, data, 1);
        }

        [Test]
        public void NoCacheEverWritten_UsesDefaults()
        {
            RemoteConfigService service = BuildFresh();
            Assert.AreEqual(1, service.GetInt("economy.reward_multiplier"));
            Assert.AreEqual(RemoteConfigCacheStatus.NoCache, service.GetDiagnostics().CacheStatus);
        }

        [Test]
        public void FreshCache_IsActivatedOnRestart()
        {
            BuildAndFetch(5);

            RemoteConfigService restarted = BuildFresh();

            Assert.AreEqual(5, restarted.GetInt("economy.reward_multiplier"));
            Assert.AreEqual(RemoteConfigCacheStatus.Fresh, restarted.GetDiagnostics().CacheStatus);
            Assert.AreEqual(RemoteConfigState.Active, restarted.State);
        }

        [Test]
        public void StaleCache_StillActivatedWhenAllowed()
        {
            BuildAndFetch(5);
            AgeCacheBy(TimeSpan.FromHours(2)); // beyond the 3600s stale threshold, within the 7-day expiration

            RemoteConfigService restarted = BuildFresh();

            Assert.AreEqual(5, restarted.GetInt("economy.reward_multiplier"));
            Assert.AreEqual(RemoteConfigCacheStatus.Stale, restarted.GetDiagnostics().CacheStatus);
        }

        [Test]
        public void ExpiredCache_FallsBackToDefaults()
        {
            BuildAndFetch(5);
            AgeCacheBy(TimeSpan.FromDays(8)); // beyond the 7-day expiration

            RemoteConfigService restarted = BuildFresh();

            Assert.AreEqual(1, restarted.GetInt("economy.reward_multiplier"));
            Assert.AreEqual(RemoteConfigCacheStatus.Expired, restarted.GetDiagnostics().CacheStatus);
        }

        [Test]
        public void CorruptCache_UnsupportedSchema_FallsBackToDefaults()
        {
            BuildAndFetch(5);

            var persistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            RemoteConfigCacheData data = persistence.Load<RemoteConfigCacheData>(CacheKey, 1, null);
            data.SchemaVersion = 99;
            persistence.Save(CacheKey, data, 1);

            RemoteConfigService restarted = BuildFresh();

            Assert.AreEqual(1, restarted.GetInt("economy.reward_multiplier"));
            Assert.AreEqual(RemoteConfigCacheStatus.Corrupt, restarted.GetDiagnostics().CacheStatus);
        }

        [Test]
        public void DifferentEnvironmentCache_IsNotUsed()
        {
            BuildAndFetch(5);

            RemoteConfigConfiguration stagingConfiguration = TestDefinitions.Configuration(
                new[] { TestDefinitions.IntDefinition("economy.reward_multiplier", 1) },
                environment: RemoteConfigEnvironment.Staging);

            ServiceRegistry registry = TestRegistryFactory.Build(_storage, out EventService _, out PersistenceService _, out TimerService _);
            var service = new RemoteConfigService(stagingConfiguration, new FakeRemoteConfigProvider());
            service.Initialize(registry);

            Assert.AreEqual(1, service.GetInt("economy.reward_multiplier"));
            Object.DestroyImmediate(stagingConfiguration);
        }
    }
}
