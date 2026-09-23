using System;
using System.Collections.Generic;
using GameFramework.RemoteConfig.LiveOps;
using GameFramework.RemoteConfig.Providers;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace GameFramework.RemoteConfig.Tests
{
    public class LiveOpsServiceTests
    {
        private static readonly LiveEventId SummerEvent = new LiveEventId("summer_event");

        private ServiceRegistry _registry;
        private FakeTimeService _time;
        private TimerService _timer;
        private RemoteConfigConfiguration _remoteConfigConfiguration;
        private LiveOpsConfiguration _liveOpsConfiguration;
        private RemoteConfigService _remoteConfig;
        private ManualLiveOpsClock _clock;
        private LiveOpsService _liveOps;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out EventService _, out PersistenceService _, out _timer);
            _time = (FakeTimeService)_registry.Get<GameFramework.Runtime.Time.ITimeService>();

            _remoteConfigConfiguration = TestDefinitions.Configuration(Array.Empty<RemoteConfigDefinition>());
            _remoteConfig = new RemoteConfigService(_remoteConfigConfiguration, new FakeRemoteConfigProvider());
            _remoteConfig.Initialize(_registry);
            _registry.Register<IRemoteConfigService>(_remoteConfig);
            _registry.MarkInitialized(typeof(IRemoteConfigService));

            _liveOpsConfiguration = TestDefinitions.LiveOpsConfig(
                new[] { TestDefinitions.LiveEvent("summer_event", "2026-06-01T00:00:00.0000000Z", "2026-06-30T00:00:00.0000000Z") },
                pollIntervalSeconds: 1f);

            _clock = new ManualLiveOpsClock(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
            _liveOps = new LiveOpsService(_liveOpsConfiguration, _clock);
            _liveOps.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _liveOps.Shutdown();
            _remoteConfig.Shutdown();
            Object.DestroyImmediate(_remoteConfigConfiguration);
            Object.DestroyImmediate(_liveOpsConfiguration);
        }

        private void AdvancePoll()
        {
            _time.UnscaledDeltaTime = 2f;
            _timer.Tick();
        }

        [Test]
        public void BeforeStart_StateIsUpcoming()
        {
            Assert.AreEqual(LiveEventState.Upcoming, _liveOps.GetState(SummerEvent));
            Assert.IsFalse(_liveOps.IsActive(SummerEvent));
        }

        [Test]
        public void DuringWindow_StateIsActive()
        {
            _clock.UtcNow = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            AdvancePoll();

            Assert.AreEqual(LiveEventState.Active, _liveOps.GetState(SummerEvent));
            CollectionAssert.Contains(_liveOps.GetActiveEvents(), SummerEvent);
        }

        [Test]
        public void AfterEnd_StateIsEnded()
        {
            _clock.UtcNow = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            AdvancePoll();

            Assert.AreEqual(LiveEventState.Ended, _liveOps.GetState(SummerEvent));
        }

        [Test]
        public void Transition_ToActive_RaisesLiveEventStarted()
        {
            var started = new List<LiveEventId>();
            _liveOps.LiveEventStarted += id => started.Add(id);

            _clock.UtcNow = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            AdvancePoll();

            CollectionAssert.Contains(started, SummerEvent);
        }

        [Test]
        public void Transition_FromActiveToEnded_RaisesLiveEventEnded()
        {
            _clock.UtcNow = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            AdvancePoll();

            var ended = new List<LiveEventId>();
            _liveOps.LiveEventEnded += id => ended.Add(id);

            _clock.UtcNow = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            AdvancePoll();

            CollectionAssert.Contains(ended, SummerEvent);
        }

        [Test]
        public void RemoteOverride_DisablesEvent()
        {
            // A fully independent fixture (its own registry/provider) so overriding the reserved
            // "liveops.summer_event.enabled" key here can't affect the shared _liveOps/_remoteConfig
            // instances used by the other tests in this fixture.
            ServiceRegistry registry = TestRegistryFactory.Build(out EventService _, out PersistenceService _, out TimerService _);
            var provider = new FakeRemoteConfigProvider();
            var remoteConfig = new RemoteConfigService(_remoteConfigConfiguration, provider);
            remoteConfig.Initialize(registry);
            registry.Register<IRemoteConfigService>(remoteConfig);
            registry.MarkInitialized(typeof(IRemoteConfigService));

            provider.NextResults.Enqueue(RemoteConfigProviderResult.Successful(1, 1, new Dictionary<string, object> { ["liveops.summer_event.enabled"] = false }));
            remoteConfig.Fetch();

            var clock = new ManualLiveOpsClock(new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc));
            var liveOps = new LiveOpsService(_liveOpsConfiguration, clock);
            liveOps.Initialize(registry);

            Assert.AreEqual(LiveEventState.Disabled, liveOps.GetState(SummerEvent));

            liveOps.Shutdown();
            remoteConfig.Shutdown();
        }
    }
}
