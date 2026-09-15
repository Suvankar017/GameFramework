using GameFramework.Core.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Core.Tests.Extensions
{
    public class GameObjectExtensionsTests
    {
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Target");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void GetOrAddComponent_WithoutExistingComponent_AddsAndReturnsIt()
        {
            var component = _gameObject.GetOrAddComponent<BoxCollider>();

            Assert.IsNotNull(component);
            Assert.AreSame(_gameObject, component.gameObject);
        }

        [Test]
        public void GetOrAddComponent_WithExistingComponent_ReturnsSameInstance()
        {
            var existing = _gameObject.AddComponent<BoxCollider>();

            var result = _gameObject.GetOrAddComponent<BoxCollider>();

            Assert.AreSame(existing, result);
            Assert.AreEqual(1, _gameObject.GetComponents<BoxCollider>().Length);
        }
    }
}
