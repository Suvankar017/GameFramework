using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.UI.Navigation.Tests
{
    /// <summary>Play Mode is required - see <c>GameFramework.UI.Tests.UIServiceTests</c>'s remarks;
    /// the same reasoning applies here since screens/popups are MonoBehaviours.</summary>
    public class NavigationServiceRegistrationTests
    {
        private NavigationService _navigation;
        private readonly List<GameObject> _templates = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            var registry = TestRegistryFactory.Build(out _, out _, out _, out _);
            _navigation = new NavigationService();
            _navigation.Initialize(registry);
        }

        [TearDown]
        public void TearDown()
        {
            _navigation.Shutdown();
            foreach (GameObject template in _templates)
            {
                if (template != null)
                {
                    UnityEngine.Object.Destroy(template);
                }
            }
            _templates.Clear();
        }

        private T CreateTemplate<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name + "Template");
            _templates.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void RegisterScreen_ThenIsRegistered_ReturnsTrue()
        {
            var id = new UIScreenId("Menu");
            _navigation.RegisterScreen(id, CreateTemplate<TestNavScreen>());

            Assert.IsTrue(_navigation.IsScreenRegistered(id));
        }

        [Test]
        public void RegisterScreen_Duplicate_Throws()
        {
            var id = new UIScreenId("Menu");
            _navigation.RegisterScreen(id, CreateTemplate<TestNavScreen>());

            Assert.Throws<InvalidOperationException>(() => _navigation.RegisterScreen(id, CreateTemplate<TestNavScreen>()));
        }

        [Test]
        public void RegisterScreen_InvalidId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _navigation.RegisterScreen(default, CreateTemplate<TestNavScreen>()));
        }

        [Test]
        public void UnregisterScreen_ThenIsRegistered_ReturnsFalse()
        {
            var id = new UIScreenId("Menu");
            _navigation.RegisterScreen(id, CreateTemplate<TestNavScreen>());

            _navigation.UnregisterScreen(id);

            Assert.IsFalse(_navigation.IsScreenRegistered(id));
        }

        [Test]
        public void IsScreenRegistered_Unknown_ReturnsFalse()
        {
            Assert.IsFalse(_navigation.IsScreenRegistered(new UIScreenId("Unknown")));
        }

        [Test]
        public void Navigate_UnknownScreen_ReturnsNotFound()
        {
            NavigationResult result = _navigation.Navigate(new UIScreenId("Unknown"));

            Assert.AreEqual(NavigationResultKind.NotFound, result.Kind);
        }

        [Test]
        public void RegisterPopup_ThenIsRegistered_ReturnsTrue()
        {
            var id = new UIPopupId("Settings");
            _navigation.RegisterPopup(id, CreateTemplate<TestNavPopup>());

            Assert.IsTrue(_navigation.IsPopupRegistered(id));
        }

        [Test]
        public void RegisterPopup_Duplicate_Throws()
        {
            var id = new UIPopupId("Settings");
            _navigation.RegisterPopup(id, CreateTemplate<TestNavPopup>());

            Assert.Throws<InvalidOperationException>(() => _navigation.RegisterPopup(id, CreateTemplate<TestNavPopup>()));
        }

        [Test]
        public void OpenPopup_UnknownPopup_ReturnsNotFound()
        {
            NavigationResult result = _navigation.OpenPopup(new UIPopupId("Unknown"));

            Assert.AreEqual(NavigationResultKind.NotFound, result.Kind);
        }
    }
}
