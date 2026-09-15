using GameFramework.Core.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Core.Tests.Extensions
{
    public class TransformExtensionsTests
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
                Object.DestroyImmediate(_root);
            }
        }

        // The multi-child, actually-removes-them case requires Object.Destroy's deferred
        // semantics, which Edit Mode disallows. See TransformExtensionsPlayModeTests
        // (Tests/Runtime) for that coverage.
        [Test]
        public void DestroyAllChildren_WithNoChildren_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _root.transform.DestroyAllChildren());
        }
    }
}
