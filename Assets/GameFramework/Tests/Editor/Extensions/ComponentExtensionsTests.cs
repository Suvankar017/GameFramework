using GameFramework.Core.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Core.Tests.Extensions
{
    public class ComponentExtensionsTests
    {
        private GameObject _gameObject;
        private Transform _transform;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Target");
            _transform = _gameObject.transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void GetOrAddComponent_WithoutExistingComponent_AddsAndReturnsIt()
        {
            var component = _transform.GetOrAddComponent<BoxCollider>();

            Assert.IsNotNull(component);
            Assert.AreSame(_gameObject, component.gameObject);
        }

        [Test]
        public void GetOrAddComponent_WithExistingComponent_ReturnsSameInstance()
        {
            var existing = _gameObject.AddComponent<BoxCollider>();

            var result = _transform.GetOrAddComponent<BoxCollider>();

            Assert.AreSame(existing, result);
        }
    }
}
