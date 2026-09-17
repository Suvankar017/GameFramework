using GameFramework.Gameplay.Pooling;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay.Tests
{
    /// <summary>Phase 5 pooling-hardening coverage: statistics, foreign/invalid release detection,
    /// and dispose-while-active - additive to the Phase 4 behavior already covered by
    /// <see cref="GameObjectPoolTests"/>.</summary>
    public class PoolHardeningTests
    {
        private GameObject _prefab;
        private GameObjectPool _pool;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("PoolHardeningTestPrefab");
            _prefab.AddComponent<RecordingPoolable>();
            _prefab.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            _pool?.Dispose();
            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab);
            }
        }

        [Test]
        public void Release_ForeignObjectNeverHandedOutByThisPool_IsRejectedWithoutCorruptingPool()
        {
            _pool = new GameObjectPool(_prefab);
            var foreign = new GameObject("ForeignObject");

            Assert.DoesNotThrow(() => _pool.Release(foreign));
            Assert.AreEqual(0, _pool.CountAll, "A foreign object must never be admitted into the pool's free list.");

            Object.DestroyImmediate(foreign);
        }

        [Test]
        public void Release_SameInstanceTwice_SecondCallIsRejectedAsForeign()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject instance = _pool.Get();
            _pool.Release(instance);

            Assert.DoesNotThrow(() => _pool.Release(instance));
            Assert.AreEqual(1, _pool.Statistics.ReleaseCount, "Only the first Release should count.");
        }

        [Test]
        public void Statistics_TracksGetReleaseAndPeakActive()
        {
            _pool = new GameObjectPool(_prefab);

            GameObject a = _pool.Get();
            GameObject b = _pool.Get();
            _pool.Release(a);

            PoolStatistics stats = _pool.Statistics;
            Assert.AreEqual(2, stats.GetCount);
            Assert.AreEqual(1, stats.ReleaseCount);
            Assert.AreEqual(2, stats.PeakActiveCount, "Peak must reflect the high-water mark, not the current count.");
            Assert.AreEqual(1, stats.CurrentActiveCount);
        }

        [Test]
        public void Statistics_TotalCreatedCount_MatchesActualInstantiateCalls()
        {
            var config = new GameObjectPoolConfig { DefaultCapacity = 1, MaxSize = 20 };
            _pool = new GameObjectPool(_prefab, config);

            _pool.Get();
            _pool.Get();
            _pool.Get();

            Assert.AreEqual(3, _pool.Statistics.TotalCreatedCount);
        }

        [Test]
        public void Statistics_MissCount_IncrementsWhenAnExternallyDestroyedInstanceIsReplaced()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject instance = _pool.Get();
            _pool.Release(instance);
            Object.DestroyImmediate(instance);

            _pool.Get();

            Assert.AreEqual(1, _pool.Statistics.MissCount);
        }

        [Test]
        public void Dispose_WithActiveInstancesOutstanding_DoesNotThrow()
        {
            _pool = new GameObjectPool(_prefab);
            _pool.Get(); // never released - simulates a caller forgetting to release before dispose

            Assert.DoesNotThrow(() => _pool.Dispose());
        }

        [Test]
        public void PrewarmStagedRoutine_EventuallyPrewarmsExactCount()
        {
            _pool = new GameObjectPool(_prefab, new GameObjectPoolConfig { DefaultCapacity = 10, MaxSize = 20 });

            var routine = _pool.PrewarmStagedRoutine(6, 2);
            int safety = 0;
            while (routine.MoveNext() && safety++ < 10)
            {
            }

            Assert.AreEqual(6, _pool.CountInactive);
            Assert.AreEqual(0, _pool.CountActive);
        }
    }
}
