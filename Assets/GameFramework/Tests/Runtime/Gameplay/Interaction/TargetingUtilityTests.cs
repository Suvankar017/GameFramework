using System.Collections.Generic;
using GameFramework.Gameplay.Interaction;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay.Tests
{
    public class TargetingUtilityTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _spawned.Clear();
        }

        private GameObject CreateSphere(Vector3 position, int layer = 0)
        {
            var go = new GameObject("Target3D");
            go.layer = layer;
            go.transform.position = position;
            go.AddComponent<SphereCollider>();
            _spawned.Add(go);
            return go;
        }

        private GameObject CreateCircle2D(Vector2 position, int layer = 0)
        {
            var go = new GameObject("Target2D");
            go.layer = layer;
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>();
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void FindTargetsInRadius_FindsCollidersWithinRange()
        {
            CreateSphere(new Vector3(1f, 0f, 0f));
            CreateSphere(new Vector3(50f, 0f, 0f)); // out of range

            var buffer = new Collider[10];
            int count = TargetingUtility.FindTargetsInRadius(Vector3.zero, 5f, Physics.AllLayers, buffer);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void FindTargetsInRadius_RespectsLayerMask()
        {
            int otherLayer = 31;
            CreateSphere(Vector3.zero, otherLayer);
            LayerMask mask = 1 << 0; // default layer only

            var buffer = new Collider[10];
            int count = TargetingUtility.FindTargetsInRadius(Vector3.zero, 5f, mask, buffer);

            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetClosest_ReturnsNearestTransform()
        {
            GameObject near = CreateSphere(new Vector3(1f, 0f, 0f));
            GameObject far = CreateSphere(new Vector3(4f, 0f, 0f));
            var buffer = new[] { near.GetComponent<Collider>(), far.GetComponent<Collider>() };

            Transform closest = TargetingUtility.GetClosest(Vector3.zero, buffer, buffer.Length);

            Assert.AreEqual(near.transform, closest);
        }

        [Test]
        public void GetClosest_EmptyCandidates_ReturnsNull()
        {
            Transform closest = TargetingUtility.GetClosest(Vector3.zero, new Collider[0], 0);

            Assert.IsNull(closest);
        }

        [Test]
        public void FindTargetsInRadius2D_FindsCollidersWithinRange()
        {
            CreateCircle2D(new Vector2(1f, 0f));
            CreateCircle2D(new Vector2(50f, 0f));

            var buffer = new Collider2D[10];
            int count = TargetingUtility.FindTargetsInRadius2D(Vector2.zero, 5f, Physics2D.AllLayers, buffer);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void GetClosest2D_ReturnsNearestTransform()
        {
            GameObject near = CreateCircle2D(new Vector2(1f, 0f));
            GameObject far = CreateCircle2D(new Vector2(4f, 0f));
            var buffer = new[] { near.GetComponent<Collider2D>(), far.GetComponent<Collider2D>() };

            Transform closest = TargetingUtility.GetClosest2D(Vector2.zero, buffer, buffer.Length);

            Assert.AreEqual(near.transform, closest);
        }

        [Test]
        public void IsWithinViewCone_TargetInFront_ReturnsTrue()
        {
            bool result = TargetingUtility.IsWithinViewCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 10f), 45f);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinViewCone_TargetBehind_ReturnsFalse()
        {
            bool result = TargetingUtility.IsWithinViewCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -10f), 45f);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinViewCone_TargetAtOrigin_ReturnsTrue()
        {
            bool result = TargetingUtility.IsWithinViewCone(Vector3.zero, Vector3.forward, Vector3.zero, 45f);

            Assert.IsTrue(result);
        }
    }
}
