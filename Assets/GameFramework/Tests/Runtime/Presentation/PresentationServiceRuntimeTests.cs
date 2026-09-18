using System.Reflection;
using GameFramework.Gameplay.Pooling;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Presentation.Tests
{
    /// <summary>
    /// Covers <see cref="PresentationService"/>'s visual-effect spawning, which genuinely needs
    /// Play Mode: the non-pooled fallback calls <c>UnityEngine.Object.Destroy(instance, lifetime)</c>,
    /// which Unity refuses outside Play Mode ("Destroy may not be called from edit mode!") - see
    /// <c>GameFramework.Presentation.Tests.PresentationServiceTests</c>'s remarks on why this one
    /// case moved here instead of EditMode, matching CLAUDE.md's Phase 10 brief, section 38.
    /// </summary>
    public class PresentationServiceRuntimeTests
    {
        private sealed class FakeTimeService : ITimeService
        {
            public float ScaledDeltaTime { get; set; }
            public float UnscaledDeltaTime { get; set; }
            public float FixedDeltaTime { get; set; }
            public float UnscaledFixedDeltaTime { get; set; }
            public float ScaledTime { get; set; }
            public float UnscaledTime { get; set; }
            public float Realtime { get; set; }
            public float TimeScale { get; private set; } = 1f;
            public bool IsPaused => false;
            public void Initialize(IServiceRegistry registry) { }
            public void Shutdown() { }
            public void SetTimeScale(float scale) => TimeScale = scale;
            public void ResetTimeScale() => TimeScale = 1f;
            public void Pause() { }
            public void Resume() { }
        }

        private ServiceRegistry _registry;
        private FakeTimeService _time;
        private TimerService _timers;
        private PoolService _pool;
        private PresentationService _presentation;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _registry = new ServiceRegistry();

            _time = new FakeTimeService();
            _registry.Register<ITimeService>(_time);
            _registry.MarkInitialized(typeof(ITimeService));

            var events = new EventService();
            _registry.Register<IEventService>(events);
            events.Initialize(_registry);
            _registry.MarkInitialized(typeof(IEventService));

            var persistence = new PersistenceService(new InMemoryPersistenceStorage(), new JsonPersistenceSerializer());
            _registry.Register<IPersistenceService>(persistence);
            persistence.Initialize(_registry);
            _registry.MarkInitialized(typeof(IPersistenceService));

            var settings = new SettingsService();
            _registry.Register<ISettingsService>(settings);
            settings.Initialize(_registry);
            _registry.MarkInitialized(typeof(ISettingsService));

            _timers = new TimerService();
            _registry.Register<ITimerService>(_timers);
            _timers.Initialize(_registry);
            _registry.MarkInitialized(typeof(ITimerService));

            _pool = new PoolService();
            _registry.Register<IPoolService>(_pool);
            _pool.Initialize(_registry);
            _registry.MarkInitialized(typeof(IPoolService));

            _prefab = new GameObject("Phase10VfxPrefab");
            _prefab.SetActive(false);

            _presentation = new PresentationService();
            _presentation.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _presentation.Shutdown();
            _pool.Shutdown();
            if (_prefab != null)
            {
                Object.Destroy(_prefab);
            }
        }

        [Test]
        public void Play_Visual_NonPooled_SpawnsARealInstance()
        {
            var definition = ScriptableObject.CreateInstance<FeedbackDefinition>();
            SetId(definition, "A");
            definition.Visual.Enabled = true;
            definition.Visual.EffectPrefab = _prefab;
            definition.Visual.UsePooling = false;
            definition.Visual.Lifetime = 100f;
            _presentation.RegisterDefinition(definition);

            int before = CountClones();
            _presentation.Play(new FeedbackId("A"));
            int after = CountClones();

            Assert.AreEqual(before + 1, after);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Play_Visual_Pooled_UsesPoolService_AndReleasesAfterLifetime()
        {
            var definition = ScriptableObject.CreateInstance<FeedbackDefinition>();
            SetId(definition, "A");
            definition.Visual.Enabled = true;
            definition.Visual.EffectPrefab = _prefab;
            definition.Visual.UsePooling = true;
            definition.Visual.Lifetime = 1f;
            _presentation.RegisterDefinition(definition);

            _presentation.Play(new FeedbackId("A"));

            bool foundPool = _pool.TryGetPool("Presentation.Effect." + _prefab.GetInstanceID(), out GameObjectPool pool);
            Assert.IsTrue(foundPool, "PresentationService should have created a pool for the effect prefab via IPoolService.");
            Assert.AreEqual(1, pool.CountActive);

            _time.ScaledDeltaTime = 1.1f;
            _timers.Tick();

            Assert.AreEqual(0, pool.CountActive, "The pooled instance should have been released back to the pool once its Lifetime elapsed.");

            Object.DestroyImmediate(definition);
        }

        private static void SetId(FeedbackDefinition definition, string id)
        {
            FieldInfo idField = typeof(FeedbackDefinition).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance);
            idField.SetValue(definition, id);
        }

        private int CountClones()
        {
            int count = 0;
            // includeInactive: true - the prefab (and therefore its clone) starts SetActive(false),
            // and the default FindObjectsOfType overload silently skips inactive objects.
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>(true))
            {
                if (go != _prefab && go.name.StartsWith("Phase10VfxPrefab"))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
