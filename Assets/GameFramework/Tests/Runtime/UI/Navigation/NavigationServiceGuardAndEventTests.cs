using System;
using System.Collections.Generic;
using GameFramework.Runtime.Events;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.UI.Navigation.Tests
{
    internal sealed class TestGuard : INavigationGuard
    {
        public NavigationGuardResult ResultToReturn = NavigationGuardResult.Allow;
        public string ReasonToReturn = "blocked-by-test-guard";
        public int EvaluateCount;
        public NavigationGuardContext LastContext;

        public NavigationGuardResult Evaluate(in NavigationGuardContext context, out string reason)
        {
            EvaluateCount++;
            LastContext = context;
            reason = ResultToReturn == NavigationGuardResult.Allow ? null : ReasonToReturn;
            return ResultToReturn;
        }
    }

    internal readonly struct TestGameFlowEvent
    {
        public readonly string Reason;
        public TestGameFlowEvent(string reason) => Reason = reason;
    }

    /// <summary>Play Mode is required - see <c>GameFramework.UI.Tests.UIServiceTests</c>'s remarks.</summary>
    public class NavigationServiceGuardAndEventTests
    {
        private NavigationService _navigation;
        private EventService _events;
        private readonly List<GameObject> _templates = new List<GameObject>();

        private readonly UIScreenId _menuId = new UIScreenId("Menu");
        private readonly UIScreenId _settingsId = new UIScreenId("Settings");
        private readonly UIPopupId _confirmPopupId = new UIPopupId("Confirm");

        [SetUp]
        public void SetUp()
        {
            var registry = TestRegistryFactory.Build(out _, out _events, out _, out _);
            _navigation = new NavigationService();
            _navigation.Initialize(registry);

            _navigation.RegisterScreen(_menuId, CreateTemplate<TestNavScreen>());
            _navigation.RegisterScreen(_settingsId, CreateTemplate<TestNavScreen>());
            _navigation.RegisterPopup(_confirmPopupId, CreateTemplate<TestNavPopup>());
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
        public void AddGuard_Blocking_RejectsNavigationAndPublishesBlockedEvent()
        {
            var guard = new TestGuard { ResultToReturn = NavigationGuardResult.Block };
            _navigation.AddGuard(guard);

            NavigationBlockedEvent? received = null;
            _events.Subscribe<NavigationBlockedEvent>(e => received = e);

            NavigationResult result = _navigation.Navigate(_menuId);

            Assert.AreEqual(NavigationResultKind.Blocked, result.Kind);
            Assert.AreEqual("blocked-by-test-guard", result.Reason);
            Assert.IsNull(_navigation.CurrentScreen);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(_menuId, received.Value.Destination);
        }

        [Test]
        public void AddGuard_Deferring_ReturnsDeferredWithoutMutatingStack()
        {
            var guard = new TestGuard { ResultToReturn = NavigationGuardResult.Defer, ReasonToReturn = "please-wait" };
            _navigation.AddGuard(guard);

            NavigationResult result = _navigation.Navigate(_menuId);

            Assert.AreEqual(NavigationResultKind.Deferred, result.Kind);
            Assert.AreEqual("please-wait", result.Reason);
            Assert.IsNull(_navigation.CurrentScreen);
        }

        [Test]
        public void RemoveGuard_StopsFurtherEvaluation()
        {
            var guard = new TestGuard { ResultToReturn = NavigationGuardResult.Block };
            _navigation.AddGuard(guard);
            _navigation.RemoveGuard(guard);

            NavigationResult result = _navigation.Navigate(_menuId);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, guard.EvaluateCount);
        }

        [Test]
        public void Guard_RunsForBackNavigationToo()
        {
            _navigation.Navigate(_menuId);
            _navigation.Navigate(_settingsId);

            var guard = new TestGuard { ResultToReturn = NavigationGuardResult.Allow };
            _navigation.AddGuard(guard);

            _navigation.NavigateBack();

            Assert.AreEqual(1, guard.EvaluateCount);
            Assert.IsTrue(guard.LastContext.IsBackNavigation);
        }

        [Test]
        public void RegisterEventMapping_EventPublished_OpensMappedPopup()
        {
            _navigation.RegisterEventMapping<TestGameFlowEvent>(_confirmPopupId);

            _events.Publish(new TestGameFlowEvent("paused"));

            Assert.IsTrue(_navigation.HasOpenPopups);
            Assert.AreEqual(_confirmPopupId, _navigation.CurrentPopupId);
        }

        [Test]
        public void RegisterEventMapping_WithOptionsFactory_PassesParametersThrough()
        {
            _navigation.RegisterEventMapping<TestGameFlowEvent>(
                _confirmPopupId,
                evt => new NavigationRequestOptions(parameters: evt.Reason));

            _events.Publish(new TestGameFlowEvent("low-health"));

            var popup = (TestNavPopup)_navigation.CurrentPopup;
            Assert.AreEqual("low-health", popup.ReceivedParameters);
        }

        [Test]
        public void RegisterEventMapping_Duplicate_Throws()
        {
            _navigation.RegisterEventMapping<TestGameFlowEvent>(_confirmPopupId);

            Assert.Throws<InvalidOperationException>(() => _navigation.RegisterEventMapping<TestGameFlowEvent>(_confirmPopupId));
        }

        [Test]
        public void UnregisterEventMapping_StopsAutoOpening()
        {
            _navigation.RegisterEventMapping<TestGameFlowEvent>(_confirmPopupId);
            _navigation.UnregisterEventMapping<TestGameFlowEvent>();

            _events.Publish(new TestGameFlowEvent("ignored"));

            Assert.IsFalse(_navigation.HasOpenPopups);
        }

        [Test]
        public void GetDiagnostics_ReflectsCurrentStacks()
        {
            _navigation.Navigate(_menuId);
            _navigation.Navigate(_settingsId);
            _navigation.OpenPopup(_confirmPopupId);

            NavigationDiagnosticsSnapshot snapshot = _navigation.GetDiagnostics();

            Assert.AreEqual(_settingsId, snapshot.CurrentScreen);
            Assert.AreEqual(2, snapshot.ScreenStack.Count);
            Assert.AreEqual(1, snapshot.PopupStack.Count);
            Assert.AreEqual(2, snapshot.RegisteredScreenCount);
            Assert.AreEqual(1, snapshot.RegisteredPopupCount);
        }
    }
}
