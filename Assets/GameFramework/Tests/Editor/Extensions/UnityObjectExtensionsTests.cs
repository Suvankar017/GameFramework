using GameFramework.Core.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Core.Tests.Extensions
{
    public class UnityObjectExtensionsTests
    {
        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [Test]
        public void IsNullOrDestroyed_WithAliveObject_ReturnsFalse()
        {
            _gameObject = new GameObject("Alive");

            Assert.IsFalse(_gameObject.IsNullOrDestroyed());
        }

        [Test]
        public void IsNullOrDestroyed_WithDestroyedObject_ReturnsTrue()
        {
            _gameObject = new GameObject("Destroyed");
            Object destroyed = _gameObject;
            Object.DestroyImmediate(_gameObject);

            Assert.IsTrue(destroyed.IsNullOrDestroyed());
        }

        [Test]
        public void IsNullOrDestroyed_WithTrueNullReference_ReturnsTrue()
        {
            Object nullObject = null;

            Assert.IsTrue(nullObject.IsNullOrDestroyed());
        }

        [Test]
        public void IsAlive_WithAliveObject_ReturnsTrue()
        {
            _gameObject = new GameObject("Alive");

            Assert.IsTrue(_gameObject.IsAlive());
        }

        [Test]
        public void IsAlive_WithDestroyedObject_ReturnsFalse()
        {
            _gameObject = new GameObject("Destroyed");
            Object destroyed = _gameObject;
            Object.DestroyImmediate(_gameObject);

            Assert.IsFalse(destroyed.IsAlive());
        }
    }
}
