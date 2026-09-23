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
    /// <summary>Phase 19: remote configuration as untrusted input - non-finite numbers, oversized
    /// values, unsupported types, corrupted/tampered cache, provider exceptions, and last-known-good
    /// preservation.</summary>
    public class RemoteConfigHardeningTests
    {
        private const string CacheKey = "GameFramework.RemoteConfig.Cache";
        private const string RangedKey = "difficulty.scale";
        private const string FlagKey = "feature.new_shop";

        private sealed class ThrowingProvider : IRemoteConfigProvider
        {
            public bool ThrowOnInitialize;

            public void Initialize(Action<bool> onComplete)
            {
                if (ThrowOnInitialize)
                {
                    throw new InvalidOperationException("SDK crashed");
                }

                onComplete(true);
            }

            public void Fetch(int currentSchemaVersion, Action<RemoteConfigProviderResult> onComplete) =>
                throw new InvalidOperationException("SDK crashed");
        }

        private InMemoryPersistenceStorage _storage;
        private RemoteConfigConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemoryPersistenceStorage();
            _configuration = TestDefinitions.Configuration(new[]
            {
                TestDefinitions.IntDefinition(RangedKey, 1, hasRange: true, min: 0, max: 10),
                TestDefinitions.FloatDefinition("economy.multiplier", 1f),
                TestDefinitions.BoolDefinition(FlagKey, false)
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_configuration);
        }

        private RemoteConfigService Build(IRemoteConfigProvider provider)
        {
            ServiceRegistry registry = TestRegistryFactory.Build(_storage, out EventService _, out PersistenceService _, out TimerService _);
            var service = new RemoteConfigService(_configuration, provider);
            service.Initialize(registry);
            return service;
        }

        private static RemoteConfigProviderResult Payload(int version, Dictionary<string, object> values) =>
            RemoteConfigProviderResult.Successful(version, 1, values);

        private static RemoteConfigFetchResultKind FetchWith(RemoteConfigService service, FakeRemoteConfigProvider provider, RemoteConfigProviderResult result)
        {
            provider.NextResults.Enqueue(result);
            RemoteConfigFetchResultKind kind = default;
            service.Fetch(r => kind = r.Kind);
            return kind;
        }

        [Test]
        public void Fetch_NaNFloat_RejectedAndLastKnownGoodKept()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService service = Build(provider);
            FetchWith(service, provider, Payload(1, new Dictionary<string, object> { ["economy.multiplier"] = 2f }));

            RemoteConfigFetchResultKind kind = FetchWith(service, provider, Payload(2, new Dictionary<string, object> { ["economy.multiplier"] = float.NaN }));

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, kind);
            Assert.AreEqual(2f, service.GetFloat("economy.multiplier"));
            Assert.AreEqual(1, service.ActiveSnapshot.Version);
        }

        [Test]
        public void Fetch_NaNOnRangedDefinitionAsDouble_Rejected()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService service = Build(provider);

            RemoteConfigFetchResultKind kind = FetchWith(service, provider, Payload(1, new Dictionary<string, object> { ["undeclared.value"] = double.PositiveInfinity }));

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, kind);
        }

        [Test]
        public void Fetch_OversizedString_Rejected()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService service = Build(provider);

            RemoteConfigFetchResultKind kind = FetchWith(service, provider, Payload(1, new Dictionary<string, object>
            {
                ["undeclared.blob"] = new string('x', RemoteConfigService.MaxStringValueLength + 1)
            }));

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, kind);
        }

        [Test]
        public void Fetch_UnsupportedValueType_Rejected()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService service = Build(provider);

            RemoteConfigFetchResultKind kind = FetchWith(service, provider, Payload(1, new Dictionary<string, object> { ["undeclared.list"] = new List<int>() }));

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, kind);
        }

        [Test]
        public void Fetch_InvalidFeatureFlagType_FlagKeepsSafeDefault()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService service = Build(provider);

            FetchWith(service, provider, Payload(1, new Dictionary<string, object> { [FlagKey] = "yes" }));

            Assert.IsFalse(service.GetBool(FlagKey, false));
        }

        [Test]
        public void Cache_InvalidTimestamp_DiscardedWithoutThrowing()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService first = Build(provider);
            FetchWith(first, provider, Payload(1, new Dictionary<string, object> { [RangedKey] = 7 }));
            first.Shutdown();

            var persistence = new PersistenceService(_storage, new JsonPersistenceSerializer());
            RemoteConfigCacheData data = persistence.Load<RemoteConfigCacheData>(CacheKey, 1, null);
            data.FetchedAtUtcTicks = long.MaxValue;
            persistence.Save(CacheKey, data, 1);

            RemoteConfigService second = null;
            Assert.DoesNotThrow(() => second = Build(new FakeRemoteConfigProvider()));
            Assert.AreEqual(1, second.GetInt(RangedKey), "Falls back to the local default.");
            Assert.AreEqual(RemoteConfigCacheStatus.Corrupt, second.GetDiagnostics().CacheStatus);
        }

        [Test]
        public void Cache_TamperedFile_DetectedAsCorruptAndDefaultsUsed()
        {
            var provider = new FakeRemoteConfigProvider();
            RemoteConfigService first = Build(provider);
            FetchWith(first, provider, Payload(1, new Dictionary<string, object> { [RangedKey] = 7 }));
            first.Shutdown();

            string raw = _storage.ReadText(CacheKey);
            _storage.WriteText(CacheKey, raw.Replace("\\\"_intValue\\\":7", "\\\"_intValue\\\":8"));
            Assert.AreNotEqual(raw, _storage.ReadText(CacheKey), "Test precondition: the cache text must have been modified.");

            RemoteConfigService second = Build(new FakeRemoteConfigProvider());

            Assert.AreEqual(1, second.GetInt(RangedKey));
            Assert.AreEqual(RemoteConfigCacheStatus.Corrupt, second.GetDiagnostics().CacheStatus);
        }

        [Test]
        public void Provider_ThrowsOnInitialize_ServiceStillStartsWithDefaults()
        {
            RemoteConfigService service = null;

            Assert.DoesNotThrow(() => service = Build(new ThrowingProvider { ThrowOnInitialize = true }));
            Assert.AreEqual(RemoteConfigState.Failed, service.State);
            Assert.AreEqual(1, service.GetInt(RangedKey));
        }

        [Test]
        public void Provider_ThrowsOnFetch_ReportsFailureAndKeepsActiveSnapshot()
        {
            RemoteConfigService service = Build(new ThrowingProvider());

            RemoteConfigFetchResultKind kind = default;
            Assert.DoesNotThrow(() => service.Fetch(r => kind = r.Kind));

            Assert.AreEqual(RemoteConfigFetchResultKind.Failed, kind);
            Assert.AreEqual(1, service.GetInt(RangedKey));
        }
    }
}
