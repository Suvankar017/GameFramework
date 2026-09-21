using System.Collections.Generic;
using GameFramework.Runtime.Events;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.UI.Navigation.Tests
{
    /// <summary>Play Mode is required - see <c>GameFramework.UI.Tests.UIServiceTests</c>'s remarks.</summary>
    public class NavigationServicePopupTests
    {
        private NavigationService _navigation;
        private EventService _events;
        private FakeGameFlowService _gameFlow;
        private readonly List<GameObject> _templates = new List<GameObject>();

        private readonly UIScreenId _menuId = new UIScreenId("Menu");
        private readonly UIPopupId _settingsPopupId = new UIPopupId("Settings");
        private readonly UIPopupId _confirmPopupId = new UIPopupId("Confirm");

        [SetUp]
        public void SetUp()
        {
            var registry = TestRegistryFactory.Build(out _, out _events, out _, out _gameFlow);
            _navigation = new NavigationService();
            _navigation.Initialize(registry);

            _navigation.RegisterScreen(_menuId, CreateTemplate<TestNavScreen>());
            _navigation.RegisterPopup(_settingsPopupId, CreateTemplate<TestNavPopup>());
            _navigation.RegisterPopup(_confirmPopupId, CreateTemplate<TestNavPopup>());

            _navigation.Navigate(_menuId);
        }

        [TearDown]
        public void TearDown()
        {
            _navigation.Shutdown();
            foreach (GameObject template in _templates)
            {
                if (template != null)
                {
                    Object.Destroy(template);
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
        public void OpenPopup_BecomesTopOfPopupStack()
        {
            NavigationResult result = _navigation.OpenPopup(_settingsPopupId);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(_navigation.HasOpenPopups);
            Assert.IsTrue(_navigation.CanNavigateBack);
        }

        [Test]
        public void OpenPopup_PublishesPopupOpenedEvent()
        {
            PopupOpenedEvent? received = null;
            _events.Subscribe<PopupOpenedEvent>(e => received = e);

            _navigation.OpenPopup(_settingsPopupId);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(_settingsPopupId, received.Value.Id);
        }

        [Test]
        public void NavigateBack_WithOpenPopup_ClosesPopupBeforeTouchingScreenStack()
        {
            _navigation.OpenPopup(_settingsPopupId);

            NavigationResult result = _navigation.NavigateBack();

            Assert.IsTrue(result.Success);
            Assert.IsFalse(_navigation.HasOpenPopups);
            Assert.AreEqual(_menuId, _navigation.CurrentScreenId);
        }

        [Test]
        public void NavigateBack_WithOpenPopup_ClosesAsCancelled()
        {
            _navigation.OpenPopup(_settingsPopupId);
            PopupClosedEvent? received = null;
            _events.Subscribe<PopupClosedEvent>(e => received = e);

            _navigation.NavigateBack();

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(UIPopupResult.Cancelled, received.Value.Result);
        }

        [Test]
        public void NestedPopups_BackClosesTopmostFirst()
        {
            _navigation.OpenPopup(_settingsPopupId);
            _navigation.OpenPopup(_confirmPopupId);

            _navigation.NavigateBack();

            Assert.IsTrue(_navigation.HasOpenPopups);
            NavigationResult second = _navigation.NavigateBack();

            Assert.IsTrue(second.Success);
            Assert.IsFalse(_navigation.HasOpenPopups);
            Assert.AreEqual(_menuId, _navigation.CurrentScreenId);
        }

        [Test]
        public void CloseTopPopup_WithResult_DeliversToCallbackAsConfirmed()
        {
            object received = null;
            UIPopupResult? uiResult = null;
            _events.Subscribe<PopupClosedEvent>(e => uiResult = e.Result);

            _navigation.OpenPopup(_settingsPopupId, new NavigationRequestOptions(resultCallback: r => received = r));
            _navigation.CloseTopPopup("confirmed-value");

            Assert.AreEqual("confirmed-value", received);
            Assert.AreEqual(UIPopupResult.Confirmed, uiResult);
        }

        [Test]
        public void CloseTopPopup_EmptyStack_ReturnsNotFound()
        {
            NavigationResult result = _navigation.CloseTopPopup();

            Assert.AreEqual(NavigationResultKind.NotFound, result.Kind);
        }

        [Test]
        public void OpenPopup_PausesGameplay_AcquiresPauseTokenFromGameFlow()
        {
            _navigation.OpenPopup(_settingsPopupId, new NavigationRequestOptions(pausesGameplay: true, pauseReason: "Settings"));

            Assert.AreEqual(1, _gameFlow.PauseGameplayCallCount);
            Assert.AreEqual("Settings", _gameFlow.LastPauseReason);
        }

        [Test]
        public void CloseTopPopup_PausedPopup_ReleasesPauseToken()
        {
            _navigation.OpenPopup(_settingsPopupId, new NavigationRequestOptions(pausesGameplay: true));

            _navigation.CloseTopPopup();

            Assert.AreEqual(1, _gameFlow.ReleasedTokenCount);
        }

        [Test]
        public void OpenPopup_WithParameters_DeliversToParameterReceiver()
        {
            var payload = new object();

            _navigation.OpenPopup(_settingsPopupId, new NavigationRequestOptions(parameters: payload));
            var popup = (TestNavPopup)_navigation.CurrentPopup;

            Assert.AreSame(payload, popup.ReceivedParameters);
        }
    }
}
