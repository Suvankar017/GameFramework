using System.Collections;
using System.Collections.Generic;
using GameFramework.Runtime.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.UI.Navigation.Tests
{
    /// <summary>Play Mode is required - see <c>GameFramework.UI.Tests.UIServiceTests</c>'s remarks.</summary>
    public class NavigationServiceStackTests
    {
        private NavigationService _navigation;
        private EventService _events;
        private FakeInputService _input;
        private readonly List<GameObject> _templates = new List<GameObject>();

        private readonly UIScreenId _menuId = new UIScreenId("Menu");
        private readonly UIScreenId _settingsId = new UIScreenId("Settings");
        private readonly UIScreenId _gameplayId = new UIScreenId("Gameplay");

        [SetUp]
        public void SetUp()
        {
            var registry = TestRegistryFactory.Build(out _, out _events, out _input, out _);
            _navigation = new NavigationService();
            _navigation.Initialize(registry);

            _navigation.RegisterScreen(_menuId, CreateTemplate<TestNavScreen>());
            _navigation.RegisterScreen(_settingsId, CreateTemplate<TestNavScreen>());
            _navigation.RegisterScreen(_gameplayId, CreateTemplate<TestNavScreen>());
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
        public void Navigate_Push_BecomesCurrentScreen()
        {
            NavigationResult result = _navigation.Navigate(_menuId);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(_menuId, _navigation.CurrentScreenId);
            Assert.IsFalse(_navigation.CanNavigateBack);
        }

        [Test]
        public void Navigate_Push_Second_StackHasBothAndCanNavigateBack()
        {
            _navigation.Navigate(_menuId);
            NavigationResult result = _navigation.Navigate(_settingsId);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(_settingsId, _navigation.CurrentScreenId);
            Assert.IsTrue(_navigation.CanNavigateBack);
        }

        [Test]
        public void Navigate_Push_PublishesScreenNavigatedEvent()
        {
            ScreenNavigatedEvent? received = null;
            _events.Subscribe<ScreenNavigatedEvent>(e => received = e);

            _navigation.Navigate(_menuId);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(UIScreenId.None, received.Value.Previous);
            Assert.AreEqual(_menuId, received.Value.Current);
            Assert.AreEqual(NavigationMode.Push, received.Value.Mode);
        }

        [Test]
        public void Navigate_WithParameters_DeliversToParameterReceiverBeforeOpened()
        {
            var payload = new object();

            _navigation.Navigate(_menuId, new NavigationRequestOptions(parameters: payload));
            var screen = (TestNavScreen)_navigation.CurrentScreen;

            Assert.AreSame(payload, screen.ReceivedParameters);
            Assert.IsTrue(screen.HasReceivedParameters);
            Assert.AreEqual(1, screen.OpenedCount);
        }

        [UnityTest]
        public IEnumerator Replace_ClosesPreviousAndOpensNewAsOnlyEntry()
        {
            _navigation.Navigate(_menuId);
            TestNavScreen menuScreen = (TestNavScreen)_navigation.CurrentScreen;

            _navigation.Replace(_settingsId);
            yield return null; // Object.Destroy is deferred to end of frame

            Assert.AreEqual(_settingsId, _navigation.CurrentScreenId);
            Assert.IsFalse(_navigation.CanNavigateBack);
            Assert.IsTrue(menuScreen == null);
        }

        [UnityTest]
        public IEnumerator Reset_ClearsHistoryAndEstablishesNewRoot()
        {
            _navigation.Navigate(_menuId);
            _navigation.Navigate(_settingsId);

            _navigation.Reset(_gameplayId);
            yield return null;

            Assert.AreEqual(_gameplayId, _navigation.CurrentScreenId);
            Assert.IsFalse(_navigation.CanNavigateBack);
        }

        [Test]
        public void NavigateBack_WithHistory_ReturnsToPreviousScreen()
        {
            _navigation.Navigate(_menuId);
            _navigation.Navigate(_settingsId);

            NavigationResult result = _navigation.NavigateBack();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(_menuId, _navigation.CurrentScreenId);
        }

        [Test]
        public void NavigateBack_DeliversResultToCallback()
        {
            object received = null;
            _navigation.Navigate(_menuId);
            _navigation.Navigate(_settingsId, new NavigationRequestOptions(resultCallback: r => received = r));

            _navigation.NavigateBack("chosen-value");

            Assert.AreEqual("chosen-value", received);
        }

        [Test]
        public void NavigateBack_AtRoot_PublishesBackRequestedAtRootEvent()
        {
            BackRequestedAtRootEvent? received = null;
            _events.Subscribe<BackRequestedAtRootEvent>(e => received = e);

            _navigation.Navigate(_menuId);
            NavigationResult result = _navigation.NavigateBack();

            Assert.AreEqual(NavigationResultKind.NotFound, result.Kind);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(_menuId, received.Value.CurrentScreen);
        }

        [Test]
        public void NavigateBack_ScreenConsumesViaBackHandler_DoesNotPopStack()
        {
            _navigation.UnregisterScreen(_menuId);
            var handlerScreen = CreateTemplate<TestBackHandlerScreen>();
            handlerScreen.ConsumeBack = true;
            _navigation.RegisterScreen(_menuId, handlerScreen);

            _navigation.Navigate(_menuId);
            _navigation.Navigate(_settingsId);

            // Force the current screen to be the back-handler instance by navigating to it last.
            _navigation.Reset(_menuId);
            NavigationResult result = _navigation.NavigateBack();

            Assert.AreEqual(NavigationResultKind.Cancelled, result.Kind);
            Assert.AreEqual(_menuId, _navigation.CurrentScreenId);
        }

        [Test]
        public void IsNavigating_WhileTransitionPlays_BlocksConcurrentNavigation()
        {
            _navigation.UnregisterScreen(_menuId);
            var transitionScreen = CreateTemplate<TestTransitionScreen>();
            transitionScreen.FramesToWait = 3;
            _navigation.RegisterScreen(_menuId, transitionScreen);

            NavigationResult first = _navigation.Navigate(_menuId);
            NavigationResult second = _navigation.Navigate(_settingsId);

            Assert.IsTrue(first.Success);
            Assert.AreEqual(NavigationResultKind.AlreadyActive, second.Kind);
            Assert.IsTrue(_navigation.IsNavigating);
        }

        [UnityTest]
        public IEnumerator IsNavigating_AfterTransitionCompletes_AllowsNavigationAgain()
        {
            _navigation.UnregisterScreen(_menuId);
            var transitionScreen = CreateTemplate<TestTransitionScreen>();
            transitionScreen.FramesToWait = 2;
            _navigation.RegisterScreen(_menuId, transitionScreen);

            _navigation.Navigate(_menuId);

            yield return null;
            yield return null;
            yield return null;

            Assert.IsFalse(_navigation.IsNavigating);
            NavigationResult result = _navigation.Navigate(_settingsId);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void Navigate_PushesAndPopsTransitionInputContext()
        {
            _navigation.Navigate(_menuId);

            // The plain (non-transition) screen has no enter-transition, so the context is pushed
            // and immediately popped again within the same synchronous call.
            Assert.AreEqual(1, _input.PushContextCount);
            Assert.AreEqual(1, _input.PopContextCount);
        }
    }
}
