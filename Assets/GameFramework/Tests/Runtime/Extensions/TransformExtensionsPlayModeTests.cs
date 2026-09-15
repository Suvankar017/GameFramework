using System.Collections;
using GameFramework.Core.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.Core.Tests.Extensions
{
    // Object.Destroy is disallowed in Edit Mode (Unity requires DestroyImmediate there), so the
    // real deferred-destruction behavior of DestroyAllChildren can only be exercised in Play Mode.
    public class TransformExtensionsPlayModeTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }

        [UnityTest]
        public IEnumerator DestroyAllChildren_WithMultipleChildren_RemovesAllOfThem()
        {
            new GameObject("ChildA").transform.SetParent(_root.transform);
            new GameObject("ChildB").transform.SetParent(_root.transform);
            new GameObject("ChildC").transform.SetParent(_root.transform);

            Assume.That(_root.transform.childCount, Is.EqualTo(3));

            _root.transform.DestroyAllChildren();

            // Object.Destroy defers actual destruction until after this frame.
            yield return null;

            Assert.AreEqual(0, _root.transform.childCount);
        }
    }
}
