using GameFramework.Gameplay.Pooling;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay.Tests
{
    public class GameObjectPoolTests
    {
        private GameObject _prefab;
        private GameObjectPool _pool;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("PoolTestPrefab");
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
        public void Get_ActivatesInstanceAndCallsActivate()
        {
            _pool = new GameObjectPool(_prefab);

            GameObject instance = _pool.Get();

            Assert.IsTrue(instance.activeSelf);
            Assert.AreEqual(1, instance.GetComponent<RecordingPoolable>().ActivateCount);
        }

        [Test]
        public void Get_NewInstance_CallsInitializeOnce()
        {
            _pool = new GameObjectPool(_prefab);

            GameObject instance = _pool.Get();

            Assert.AreEqual(1, instance.GetComponent<RecordingPoolable>().InitializeCount);
        }

        [Test]
        public void Release_DeactivatesInstanceAndCallsDeactivate()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject instance = _pool.Get();

            _pool.Release(instance);

            Assert.IsFalse(instance.activeSelf);
            Assert.AreEqual(1, instance.GetComponent<RecordingPoolable>().DeactivateCount);
        }

        [Test]
        public void Get_ReleaseThenGetAgain_ReusesInstanceWithoutReinitializing()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject first = _pool.Get();
            RecordingPoolable recorder = first.GetComponent<RecordingPoolable>();
            _pool.Release(first);

            GameObject second = _pool.Get();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, recorder.InitializeCount, "Initialize must fire only once across reuse.");
            Assert.AreEqual(2, recorder.ActivateCount);
        }

        [Test]
        public void Prewarm_PopulatesInactivePoolWithoutAnyActiveInstances()
        {
            var config = new GameObjectPoolConfig { PrewarmCount = 5, DefaultCapacity = 5, MaxSize = 20 };

            _pool = new GameObjectPool(_prefab, config);

            Assert.AreEqual(5, _pool.CountInactive);
            Assert.AreEqual(0, _pool.CountActive);
            Assert.AreEqual(5, _pool.CountAll);
        }

        [Test]
        public void Get_BeyondDefaultCapacity_CreatesAdditionalInstances()
        {
            var config = new GameObjectPoolConfig { DefaultCapacity = 1, MaxSize = 20 };
            _pool = new GameObjectPool(_prefab, config);

            GameObject a = _pool.Get();
            GameObject b = _pool.Get();
            GameObject c = _pool.Get();

            Assert.AreNotSame(a, b);
            Assert.AreNotSame(b, c);
            Assert.AreEqual(3, _pool.CountActive);
        }

        [Test]
        public void Release_BeyondMaxSize_DestroysExcessInstance()
        {
            var config = new GameObjectPoolConfig { DefaultCapacity = 1, MaxSize = 1 };
            _pool = new GameObjectPool(_prefab, config);
            GameObject a = _pool.Get();
            GameObject b = _pool.Get();

            _pool.Release(a);
            _pool.Release(b); // exceeds MaxSize of 1 inactive instance

            Assert.AreEqual(1, _pool.CountInactive);
            Assert.AreEqual(1, _pool.CountAll);
        }

        [Test]
        public void Release_CalledTwiceOnSameInstance_DoesNotThrowOrCorruptPool()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject instance = _pool.Get();
            _pool.Release(instance);

            Assert.DoesNotThrow(() => _pool.Release(instance));
            Assert.AreEqual(1, _pool.CountInactive);
        }

        [Test]
        public void Release_Null_DoesNotThrow()
        {
            _pool = new GameObjectPool(_prefab);

            Assert.DoesNotThrow(() => _pool.Release(null));
        }

        [Test]
        public void Get_AfterInstanceDestroyedExternally_TransparentlyReplacesIt()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject instance = _pool.Get();
            _pool.Release(instance);
            Object.DestroyImmediate(instance); // simulates external destruction of a pooled object

            GameObject replacement = _pool.Get();

            Assert.IsNotNull(replacement);
            Assert.IsTrue(replacement.activeSelf);
        }

        [Test]
        public void Get_WithPositionAndRotation_PlacesInstance()
        {
            _pool = new GameObjectPool(_prefab);
            var position = new Vector3(1f, 2f, 3f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject instance = _pool.Get(position, rotation);

            Assert.AreEqual(position, instance.transform.position);
            Assert.Less(Quaternion.Angle(rotation, instance.transform.rotation), 0.01f);
        }

        [Test]
        public void Dispose_DestroysInactiveInstances()
        {
            _pool = new GameObjectPool(_prefab);
            GameObject instance = _pool.Get();
            _pool.Release(instance);

            _pool.Dispose();

            Assert.AreEqual(0, _pool.CountAll);
        }
    }
}
