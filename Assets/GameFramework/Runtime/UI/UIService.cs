using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameFramework.UI
{
    /// <summary>
    /// Default <see cref="IUIService"/>. Builds its own layered-canvas hierarchy under a
    /// <c>DontDestroyOnLoad</c> root at <see cref="Initialize"/> — the same self-contained pattern
    /// <c>AudioService</c> uses — so a game gets working UI infrastructure with no scene setup.
    /// Ensures an <see cref="EventSystem"/> exists (creating one if the scene has none), since
    /// without it no UGUI element receives input at all. Every layer's canvas render mode is
    /// controlled by the <see cref="UICanvasConfig"/> passed to the constructor — Screen Space -
    /// Overlay by default (true zero-setup), or Screen Space - Camera when a game needs UI to share
    /// a camera stack with the world; see <see cref="UICanvasConfig"/>.
    /// </summary>
    public sealed class UIService : IUIService
    {
        private const string LogCategory = "UI";
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;

        private readonly Dictionary<UILayer, Canvas> _canvases = new Dictionary<UILayer, Canvas>();
        private readonly List<UIScreen> _screenStack = new List<UIScreen>();
        private readonly List<UIPopup> _popups = new List<UIPopup>();

        private readonly UICanvasConfig _config;

        private ILoggingService _log;
        private GameObject _root;
        private GameObject _createdEventSystem;
        private GameObject _modalBlocker;

        /// <summary>Builds every layer as Screen Space - Overlay — the framework's original
        /// zero-setup default. Use <see cref="UIService(UICanvasConfig)"/> for Screen Space - Camera.</summary>
        public UIService() : this(new UICanvasConfig())
        {
        }

        public UIService(UICanvasConfig config)
        {
            _config = config ?? new UICanvasConfig();
        }

        public void Initialize(IServiceRegistry registry)
        {
            registry.TryGet(out _log);

            _root = new GameObject("UIService (GameFramework)");
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_root);
            }

            EnsureEventSystemExists();
            BuildLayers();
        }

        public void Shutdown()
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                _popups[i].InternalClose(UIPopupResult.None);
            }
            _popups.Clear();

            for (int i = _screenStack.Count - 1; i >= 0; i--)
            {
                _screenStack[i].InternalClose();
            }
            _screenStack.Clear();

            UIObjectUtility.DestroySafely(_createdEventSystem);
            UIObjectUtility.DestroySafely(_root);

            _createdEventSystem = null;
            _root = null;
            _modalBlocker = null;
            _canvases.Clear();
        }

        public Transform GetLayerRoot(UILayer layer) => _canvases[layer].transform;

        public T OpenScreen<T>(T prefab, Action<T> onBeforeOpen = null) where T : UIScreen
        {
            Guard.NotNull(prefab, nameof(prefab));

            UIScreen currentTop = CurrentScreen;
            currentTop?.InternalHide();

            T instance = Object.Instantiate(prefab, GetLayerRoot(prefab.Layer));
            instance.Owner = this;
            onBeforeOpen?.Invoke(instance);
            _screenStack.Add(instance);
            instance.InternalOpen();

            return instance;
        }

        public void CloseScreen(UIScreen screen)
        {
            if (screen == null || CurrentScreen != screen)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "CloseScreen called on a screen that is not the current top screen; ignored.");
                return;
            }

            CloseTopScreen();
        }

        public void CloseTopScreen()
        {
            if (_screenStack.Count == 0)
            {
                return;
            }

            UIScreen top = _screenStack[_screenStack.Count - 1];
            _screenStack.RemoveAt(_screenStack.Count - 1);
            top.InternalClose();

            CurrentScreen?.InternalShow();
        }

        public void PopToRoot()
        {
            while (_screenStack.Count > 1)
            {
                CloseTopScreen();
            }
        }

        public UIScreen CurrentScreen => _screenStack.Count > 0 ? _screenStack[_screenStack.Count - 1] : null;

        public T OpenPopup<T>(T prefab, Action<T> onBeforeOpen = null) where T : UIPopup
        {
            Guard.NotNull(prefab, nameof(prefab));

            UILayer layer = prefab.IsModal ? UILayer.Modal : UILayer.Popup;
            Transform layerRoot = GetLayerRoot(layer);

            if (prefab.IsModal)
            {
                GameObject blocker = EnsureModalBlocker(layerRoot);
                blocker.SetActive(true);
                blocker.transform.SetAsLastSibling();
            }

            T instance = Object.Instantiate(prefab, layerRoot);
            instance.Owner = this;
            onBeforeOpen?.Invoke(instance);
            _popups.Add(instance);
            instance.InternalOpen();

            return instance;
        }

        public void ClosePopup(UIPopup popup, UIPopupResult result = UIPopupResult.None)
        {
            if (popup == null || !_popups.Remove(popup))
            {
                _log?.Log(LogLevel.Warning, LogCategory, "ClosePopup called on a popup that is not currently open; ignored.");
                return;
            }

            popup.InternalClose(result);
            RefreshModalBlocker();
        }

        public void CloseTopPopup(UIPopupResult result = UIPopupResult.None)
        {
            if (_popups.Count == 0)
            {
                return;
            }

            ClosePopup(_popups[_popups.Count - 1], result);
        }

        public UIPopup CurrentPopup => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        private void RefreshModalBlocker()
        {
            UIPopup topmostModal = null;
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                if (_popups[i].IsModal)
                {
                    topmostModal = _popups[i];
                    break;
                }
            }

            if (topmostModal == null)
            {
                if (_modalBlocker != null)
                {
                    _modalBlocker.SetActive(false);
                }

                return;
            }

            GameObject blocker = EnsureModalBlocker(topmostModal.transform.parent);
            blocker.SetActive(true);
            blocker.transform.SetSiblingIndex(topmostModal.transform.GetSiblingIndex());
        }

        private GameObject EnsureModalBlocker(Transform parent)
        {
            if (_modalBlocker == null)
            {
                _modalBlocker = new GameObject("ModalBlocker (GameFramework)");
                RectTransform rect = _modalBlocker.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = _modalBlocker.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f); // fully transparent - still blocks raycasts
            }

            _modalBlocker.transform.SetParent(parent, worldPositionStays: false);
            return _modalBlocker;
        }

        private void BuildLayers()
        {
            bool useCameraMode = _config.RenderMode == UIRenderMode.ScreenSpaceCamera;
            if (useCameraMode && _config.WorldCamera == null)
            {
                _log?.Log(LogLevel.Warning, LogCategory,
                    "UICanvasConfig.RenderMode is ScreenSpaceCamera but WorldCamera is null; " +
                    "falling back to Screen Space - Overlay for every layer.");
                useCameraMode = false;
            }

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerGo = new GameObject(layer.ToString());
                layerGo.transform.SetParent(_root.transform, worldPositionStays: false);

                var canvas = layerGo.AddComponent<Canvas>();
                canvas.sortingOrder = (int)layer;

                if (useCameraMode)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = _config.WorldCamera;
                    canvas.planeDistance = _config.PlaneDistance;
                }
                else
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                var scaler = layerGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                layerGo.AddComponent<GraphicRaycaster>();

                _canvases[layer] = canvas;
            }
        }

        private void EnsureEventSystemExists()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            _createdEventSystem = new GameObject("EventSystem (GameFramework)");
            _createdEventSystem.AddComponent<EventSystem>();
            _createdEventSystem.AddComponent<StandaloneInputModule>();

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_createdEventSystem);
            }
        }
    }
}
