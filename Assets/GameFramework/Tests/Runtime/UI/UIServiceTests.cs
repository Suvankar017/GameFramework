using System.Collections;
using System.Collections.Generic;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace GameFramework.UI.Tests
{
    /// <summary>
    /// Play Mode is required: <see cref="UIScreen"/>/<see cref="UIPopup"/> subclasses used as test
    /// doubles are MonoBehaviours, and a script defined in an Editor-only test assembly cannot be
    /// added to a GameObject via <c>AddComponent</c> — only a Play-Mode-capable assembly can host
    /// them. This mirrors the project's own convention (see <c>GameBootstrapperPlayModeTests</c>).
    /// </summary>
    public class UIServiceTests
    {
        private UIService _ui;
        private readonly List<GameObject> _templates = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _ui = new UIService();
            _ui.Initialize(new ServiceRegistry());
        }

        [TearDown]
        public void TearDown()
        {
            _ui.Shutdown();
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
        public void Initialize_CreatesEventSystemWhenNoneExists()
        {
            Assert.IsNotNull(EventSystem.current);
        }

        [Test]
        public void GetLayerRoot_UsesLayerIntAsSortingOrder()
        {
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                Canvas canvas = _ui.GetLayerRoot(layer).GetComponent<Canvas>();
                Assert.AreEqual((int)layer, canvas.sortingOrder);
            }
        }

        [Test]
        public void OpenScreen_OpensAndBecomesCurrentScreen()
        {
            TestScreen screen = _ui.OpenScreen(CreateTemplate<TestScreen>());

            Assert.AreEqual(UIScreenState.Opened, screen.State);
            Assert.AreEqual(screen, _ui.CurrentScreen);
            Assert.AreEqual(1, screen.OpenedCount);
        }

        [Test]
        public void OpenScreen_Second_HidesFirstScreen()
        {
            TestScreen first = _ui.OpenScreen(CreateTemplate<TestScreen>());
            TestScreen second = _ui.OpenScreen(CreateTemplate<TestScreen>());

            Assert.AreEqual(UIScreenState.Hidden, first.State);
            Assert.AreEqual(1, first.HiddenCount);
            Assert.AreEqual(UIScreenState.Opened, second.State);
            Assert.AreEqual(second, _ui.CurrentScreen);
        }

        [UnityTest]
        public IEnumerator CloseTopScreen_RestoresPreviousScreenAndDestroysClosedOne()
        {
            TestScreen first = _ui.OpenScreen(CreateTemplate<TestScreen>());
            TestScreen second = _ui.OpenScreen(CreateTemplate<TestScreen>());

            _ui.CloseTopScreen();
            yield return null; // Object.Destroy is deferred to end of frame

            Assert.AreEqual(UIScreenState.Opened, first.State);
            Assert.AreEqual(1, first.ShownCount);
            Assert.AreEqual(first, _ui.CurrentScreen);
            Assert.IsTrue(second == null);
        }

        [Test]
        public void CloseTopScreen_EmptyStack_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ui.CloseTopScreen());
        }

        [Test]
        public void CloseScreen_NotCurrentTop_IsIgnored()
        {
            TestScreen first = _ui.OpenScreen(CreateTemplate<TestScreen>());
            TestScreen second = _ui.OpenScreen(CreateTemplate<TestScreen>());

            _ui.CloseScreen(first); // not the top

            Assert.AreEqual(second, _ui.CurrentScreen);
        }

        [Test]
        public void Screen_Close_ClosesThroughOwner()
        {
            TestScreen screen = _ui.OpenScreen(CreateTemplate<TestScreen>());

            screen.Close();

            Assert.IsNull(_ui.CurrentScreen);
        }

        [Test]
        public void PopToRoot_LeavesOnlyFirstScreen()
        {
            TestScreen first = _ui.OpenScreen(CreateTemplate<TestScreen>());
            _ui.OpenScreen(CreateTemplate<TestScreen>());
            _ui.OpenScreen(CreateTemplate<TestScreen>());

            _ui.PopToRoot();

            Assert.AreEqual(first, _ui.CurrentScreen);
        }

        [Test]
        public void OpenPopup_Modal_ActivatesBlockerBehindPopup()
        {
            TestPopup popup = _ui.OpenPopup(CreateTemplate<TestPopup>());

            Transform blocker = _ui.GetLayerRoot(UILayer.Modal).Find("ModalBlocker (GameFramework)");

            Assert.IsNotNull(blocker);
            Assert.IsTrue(blocker.gameObject.activeSelf);
            Assert.Less(blocker.GetSiblingIndex(), popup.transform.GetSiblingIndex());
        }

        [Test]
        public void OpenPopup_NonModal_DoesNotCreateBlocker()
        {
            TestPopup template = CreateTemplate<TestPopup>();

            // Configure as non-modal via reflection since _isModal is a private serialized field
            // (there is no public API to set it - authoring happens in the Inspector).
            var field = typeof(UIPopup).GetField("_isModal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(template, false);

            _ui.OpenPopup(template);

            Transform blocker = _ui.GetLayerRoot(UILayer.Modal).Find("ModalBlocker (GameFramework)");
            Assert.IsNull(blocker);
        }

        [Test]
        public void ClosePopup_LastModal_DeactivatesBlocker()
        {
            TestPopup popup = _ui.OpenPopup(CreateTemplate<TestPopup>());

            _ui.ClosePopup(popup);

            Transform blocker = _ui.GetLayerRoot(UILayer.Modal).Find("ModalBlocker (GameFramework)");
            Assert.IsFalse(blocker.gameObject.activeSelf);
            Assert.IsNull(_ui.CurrentPopup);
        }

        [Test]
        public void ClosePopup_WithAnotherModalBelow_KeepsBlockerActive()
        {
            TestPopup first = _ui.OpenPopup(CreateTemplate<TestPopup>());
            TestPopup second = _ui.OpenPopup(CreateTemplate<TestPopup>());

            _ui.ClosePopup(second);

            Transform blocker = _ui.GetLayerRoot(UILayer.Modal).Find("ModalBlocker (GameFramework)");
            Assert.IsTrue(blocker.gameObject.activeSelf);
            Assert.AreEqual(first, _ui.CurrentPopup);
        }

        [Test]
        public void CloseTopPopup_EmptyStack_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ui.CloseTopPopup());
        }

        [Test]
        public void Popup_Close_PassesResultToOnClosedAndClosedEvent()
        {
            TestPopup popup = _ui.OpenPopup(CreateTemplate<TestPopup>());
            UIPopupResult? eventResult = null;
            popup.Closed += r => eventResult = r;

            popup.Close(UIPopupResult.Confirmed);

            Assert.AreEqual(UIPopupResult.Confirmed, eventResult);
        }

        [Test]
        public void ClosePopup_NotOpen_IsIgnored()
        {
            TestPopup popup = CreateTemplate<TestPopup>();

            Assert.DoesNotThrow(() => _ui.ClosePopup(popup));
        }
    }
}
