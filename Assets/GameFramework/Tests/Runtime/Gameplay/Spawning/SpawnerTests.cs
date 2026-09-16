using System.Reflection;
using GameFramework.Gameplay.Pooling;
using GameFramework.Gameplay.Spawning;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay.Tests
{
    public class SpawnerTests
    {
        private GameObject _prefab;
        private GameObject _spawnerGo;
        private Spawner _spawner;
        private FakeTimeService _time;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("SpawnTestPrefab");
            _prefab.SetActive(false);

            _spawnerGo = new GameObject("SpawnerUnderTest");
            _spawner = _spawnerGo.AddComponent<Spawner>();
            _time = new FakeTimeService();
            _spawner.Initialize(_time);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_spawnerGo);
            Object.DestroyImmediate(_prefab);
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = typeof(Spawner).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(_spawner, value);
        }

        [Test]
        public void TrySpawn_WithPrefabConfigured_Succeeds()
        {
            SetField("_prefab", _prefab);

            SpawnResult result = _spawner.TrySpawn();

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Instance);
            Assert.AreEqual(1, _spawner.ActiveCount);
            Assert.AreEqual(1, _spawner.TotalSpawnedCount);

            Object.DestroyImmediate(result.Instance);
        }

        [Test]
        public void TrySpawn_NoPrefabConfigured_FailsWithMissingPrefab()
        {
            SpawnResult result = _spawner.TrySpawn();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(SpawnFailureReason.MissingPrefab, result.FailureReason);
        }

        [Test]
        public void TrySpawn_MaxActiveInstancesReached_FailsWithLimitReached()
        {
            SetField("_prefab", _prefab);
            SetField("_maxActiveInstances", 1);
            SpawnResult first = _spawner.TrySpawn();

            SpawnResult second = _spawner.TrySpawn();

            Assert.IsTrue(first.Success);
            Assert.IsFalse(second.Success);
            Assert.AreEqual(SpawnFailureReason.LimitReached, second.FailureReason);

            Object.DestroyImmediate(first.Instance);
        }

        [Test]
        public void TrySpawn_MaxTotalInstancesReached_FailsEvenAfterDespawn()
        {
            SetField("_prefab", _prefab);
            SetField("_maxTotalInstances", 1);
            SpawnResult first = _spawner.TrySpawn();
            _spawner.Despawn(first.Instance);

            SpawnResult second = _spawner.TrySpawn();

            Assert.IsFalse(second.Success);
            Assert.AreEqual(SpawnFailureReason.LimitReached, second.FailureReason);
        }

        [Test]
        public void TrySpawn_DuringCooldown_FailsWithCooldown()
        {
            SetField("_prefab", _prefab);
            SetField("_spawnCooldownSeconds", 5f);
            _time.ScaledTime = 10f;
            SpawnResult first = _spawner.TrySpawn();

            _time.ScaledTime = 11f; // only 1s elapsed, cooldown is 5s
            SpawnResult second = _spawner.TrySpawn();

            Assert.IsTrue(first.Success);
            Assert.IsFalse(second.Success);
            Assert.AreEqual(SpawnFailureReason.Cooldown, second.FailureReason);

            Object.DestroyImmediate(first.Instance);
        }

        [Test]
        public void TrySpawn_AfterCooldownElapses_Succeeds()
        {
            SetField("_prefab", _prefab);
            SetField("_spawnCooldownSeconds", 5f);
            _time.ScaledTime = 10f;
            SpawnResult first = _spawner.TrySpawn();

            _time.ScaledTime = 16f;
            SpawnResult second = _spawner.TrySpawn();

            Assert.IsTrue(second.Success);

            Object.DestroyImmediate(first.Instance);
            Object.DestroyImmediate(second.Instance);
        }

        [Test]
        public void Despawn_RemovesFromActiveTracking()
        {
            SetField("_prefab", _prefab);
            SpawnResult result = _spawner.TrySpawn();

            _spawner.Despawn(result.Instance);

            Assert.AreEqual(0, _spawner.ActiveCount);
        }

        [Test]
        public void Spawned_EventFires_OnSuccessfulSpawn()
        {
            SetField("_prefab", _prefab);
            GameObject spawnedInstance = null;
            _spawner.Spawned += go => spawnedInstance = go;

            SpawnResult result = _spawner.TrySpawn();

            Assert.AreSame(result.Instance, spawnedInstance);
            Object.DestroyImmediate(result.Instance);
        }

        [Test]
        public void TrySpawn_WithPooledProvider_UsesThePool()
        {
            SetField("_prefab", _prefab);
            var pool = new GameObjectPool(_prefab);
            _spawner.SetProvider(new PooledSpawnProvider(pool));

            SpawnResult first = _spawner.TrySpawn();
            _spawner.Despawn(first.Instance);
            SpawnResult second = _spawner.TrySpawn();

            Assert.AreSame(first.Instance, second.Instance, "Pooled provider should reuse the released instance.");

            pool.Dispose();
        }
    }
}
