using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.GameFlow;
using GameFramework.Input;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.UI.Navigation
{
    /// <summary>Default <see cref="INavigationService"/>. See that interface's remarks for the
    /// overall design; this type's own remarks below cover implementation decisions CLAUDE.md's
    /// Phase 12 brief asked to be documented explicitly.
    ///
    /// <b>Concurrency policy</b> (section 21): a navigation request made while
    /// <see cref="IsNavigating"/> is true is rejected outright
    /// (<see cref="NavigationResultKind.AlreadyActive"/>), never queued or silently cancelled - the
    /// simplest deterministic policy, matching this framework's existing "commands never throw,
    /// they return a result" style. This also means a navigation method called synchronously from
    /// inside a lifecycle hook this service itself just triggered (<c>UIScreen.OnShown</c>/
    /// <c>OnOpened</c>/<c>OnClosed</c>/<c>OnHidden</c>, or an <see cref="IUINavigationBackHandler"/>
    /// callback) is rejected the same way, since <see cref="IsNavigating"/> is still true for the
    /// duration of the call that triggered the hook - the same deliberate re-entrancy guard
    /// <c>GameFlow.GameFlowService</c>/<c>Tutorials.TutorialService</c> already apply to a command
    /// issued from inside one of their own event handlers. Defer such a follow-up call by one frame
    /// (a coroutine) instead of calling back in directly - see the Phase12Demo sample's
    /// <c>CarSelectionScreen</c> for a worked example.
    ///
    /// <b>Contexts</b> (section 15): this implementation deliberately does not give every
    /// "context" (Main Menu vs. Gameplay vs. Results) its own persisted, independently-resumable
    /// stack - Phase 3's <see cref="UIScreen"/> destroys its GameObject on close, so there is no
    /// existing seam to keep a hidden context's screens alive to return to later without either
    /// duplicating Phase 3's lifecycle or silently losing screen state anyway. Use
    /// <see cref="Reset"/> to move between top-level flows instead (e.g. Main Menu -> Gameplay,
    /// where "back" should never return to the menu) - the same outcome, achieved with the
    /// lifecycle Phase 3 already guarantees rather than a new one layered awkwardly on top of it.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private const string LogCategory = "UI.Navigation";
        private const string DefaultBackActionName = "Cancel";
        private const string TransitionInputContextName = "UI.Navigation.Transition";

        private readonly UIScreenRegistry _screens = new UIScreenRegistry();
        private readonly UIPopupRegistry _popups = new UIPopupRegistry();
        private readonly List<NavigationEntry> _screenStack = new List<NavigationEntry>();
        private readonly List<PopupEntry> _popupStack = new List<PopupEntry>();
        private readonly List<INavigationGuard> _guards = new List<INavigationGuard>();
        private readonly Dictionary<Type, Action> _eventMappingUnsubscribe = new Dictionary<Type, Action>();

        private IUIService _ui;
        private ILoggingService _log;
        private IInputService _input;
        private IGameFlowService _gameFlow;
        private IEventService _events;

        private GameObject _driverRoot;
        private NavigationCoroutineRunner _coroutineRunner;
        private bool _transitionContextPushed;

        public bool IsNavigating { get; private set; }
        public bool CanNavigateBack => _popupStack.Count > 0 || _screenStack.Count > 1;
        public bool HasOpenPopups => _popupStack.Count > 0;

        public UIScreenId CurrentScreenId => _screenStack.Count > 0 ? _screenStack[_screenStack.Count - 1].Id : UIScreenId.None;
        public UIScreen CurrentScreen => _screenStack.Count > 0 ? _screenStack[_screenStack.Count - 1].Screen : null;

        public UIPopupId CurrentPopupId => _popupStack.Count > 0 ? _popupStack[_popupStack.Count - 1].Id : UIPopupId.None;
        public UIPopup CurrentPopup => _popupStack.Count > 0 ? _popupStack[_popupStack.Count - 1].Popup : null;

        public event Action<UIScreenId, UIScreenId> ScreenChanged;

        public void Initialize(IServiceRegistry registry)
        {
            // Hard dependency: this layer orchestrates Phase 3's screen/popup stack, it doesn't
            // merely soft-look-up an optional channel (compare Presentation's soft channels).
            _ui = registry.Get<IUIService>();

            registry.TryGet(out _log);
            registry.TryGet(out _input);
            registry.TryGet(out _gameFlow);
            registry.TryGet(out _events);

            _driverRoot = new GameObject("NavigationService (GameFramework)");
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_driverRoot);
            }

            _coroutineRunner = _driverRoot.AddComponent<NavigationCoroutineRunner>();

            NavigationBackButtonDriver backDriver = _driverRoot.AddComponent<NavigationBackButtonDriver>();
            backDriver.Initialize(this, _input, DefaultBackActionName);
        }

        public void Shutdown()
        {
            foreach (Action unsubscribe in _eventMappingUnsubscribe.Values)
            {
                unsubscribe();
            }
            _eventMappingUnsubscribe.Clear();

            for (int i = _popupStack.Count - 1; i >= 0; i--)
            {
                _popupStack[i].PauseToken?.Release();
            }
            _popupStack.Clear();
            _screenStack.Clear();
            _guards.Clear();
            _screens.Clear();
            _popups.Clear();

            if (_driverRoot != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_driverRoot);
                }
                else
                {
                    Object.DestroyImmediate(_driverRoot);
                }

                _driverRoot = null;
            }

            _coroutineRunner = null;
        }

        public void RegisterScreen(UIScreenId id, UIScreen prefab) => _screens.Register(id, prefab);
        public void UnregisterScreen(UIScreenId id) => _screens.Unregister(id);
        public bool IsScreenRegistered(UIScreenId id) => _screens.IsRegistered(id);

        public void RegisterPopup(UIPopupId id, UIPopup prefab) => _popups.Register(id, prefab);
        public void UnregisterPopup(UIPopupId id) => _popups.Unregister(id);
        public bool IsPopupRegistered(UIPopupId id) => _popups.IsRegistered(id);

        public NavigationResult Navigate(UIScreenId destination, NavigationRequestOptions options = default) =>
            ExecuteScreenNavigation(destination, NavigationMode.Push, options);

        public NavigationResult Replace(UIScreenId destination, NavigationRequestOptions options = default) =>
            ExecuteScreenNavigation(destination, NavigationMode.Replace, options);

        public NavigationResult Reset(UIScreenId destination, NavigationRequestOptions options = default) =>
            ExecuteScreenNavigation(destination, NavigationMode.Reset, options);

        private NavigationResult ExecuteScreenNavigation(UIScreenId destination, NavigationMode mode, NavigationRequestOptions options)
        {
            if (IsNavigating)
            {
                return NavigationResult.AlreadyActive();
            }

            if (!destination.IsValid)
            {
                return NavigationResult.NotFound("Destination screen id is not valid.");
            }

            if (!_screens.TryGet(destination, out UIScreen prefab))
            {
                return NavigationResult.NotFound($"Screen '{destination}' is not registered.");
            }

            UIScreenId from = CurrentScreenId;
            NavigationGuardResult guardResult = EvaluateGuards(from, destination, mode, isBackNavigation: false, out string reason);
            if (guardResult == NavigationGuardResult.Block)
            {
                _events?.Publish(new NavigationBlockedEvent(destination, NavigationResultKind.Blocked, reason));
                return NavigationResult.Block(reason);
            }
            if (guardResult == NavigationGuardResult.Defer)
            {
                return NavigationResult.Deferred(reason);
            }

            IsNavigating = true;
            PushTransitionInputBlock();

            if (mode == NavigationMode.Replace && _screenStack.Count > 0)
            {
                CloseTopScreenEntry(result: null);
            }
            else if (mode == NavigationMode.Reset)
            {
                while (_screenStack.Count > 0)
                {
                    CloseTopScreenEntry(result: null);
                }
            }

            UIScreen instance = _ui.OpenScreen(prefab, screen => ApplyParameters(screen, options.Parameters));
            _screenStack.Add(new NavigationEntry { Id = destination, Screen = instance, ResultCallback = options.ResultCallback });

            _events?.Publish(new ScreenNavigatedEvent(from, destination, mode));
            ScreenChanged?.Invoke(from, destination);

            FinishNavigation(instance);

            return NavigationResult.Ok();
        }

        public NavigationResult NavigateBack(object result = null)
        {
            if (IsNavigating)
            {
                return NavigationResult.AlreadyActive();
            }

            if (_popupStack.Count > 0)
            {
                IsNavigating = true;
                NavigationResult popupResult = ClosePopupEntry(_popupStack.Count - 1, result, UIPopupResult.Cancelled);
                IsNavigating = false;
                return popupResult;
            }

            if (CurrentScreen is IUINavigationBackHandler handler && handler.OnBackRequested())
            {
                return NavigationResult.Cancel("Consumed by the current screen's IUINavigationBackHandler.");
            }

            if (_screenStack.Count > 1)
            {
                return PopScreen(result);
            }

            _events?.Publish(new BackRequestedAtRootEvent(CurrentScreenId));
            return NavigationResult.NotFound("No navigation history remains; BackRequestedAtRootEvent published for the game to handle.");
        }

        private NavigationResult PopScreen(object result)
        {
            UIScreenId from = CurrentScreenId;
            NavigationGuardResult guardResult = EvaluateGuards(from, UIScreenId.None, NavigationMode.Pop, isBackNavigation: true, out string reason);
            if (guardResult == NavigationGuardResult.Block)
            {
                _events?.Publish(new NavigationBlockedEvent(UIScreenId.None, NavigationResultKind.Blocked, reason));
                return NavigationResult.Block(reason);
            }
            if (guardResult == NavigationGuardResult.Defer)
            {
                return NavigationResult.Deferred(reason);
            }

            IsNavigating = true;
            PushTransitionInputBlock();

            CloseTopScreenEntry(result);

            UIScreenId current = CurrentScreenId;
            _events?.Publish(new ScreenNavigatedEvent(from, current, NavigationMode.Pop));
            ScreenChanged?.Invoke(from, current);

            IsNavigating = false;
            PopTransitionInputBlock();

            return NavigationResult.Ok();
        }

        /// <summary>Pops and closes <see cref="_screenStack"/>'s own top entry, invoking its result
        /// callback. Assumes it is also the current top of <see cref="IUIService"/>'s physical stack,
        /// which is always true since this service is the only caller driving that stack.</summary>
        private void CloseTopScreenEntry(object result)
        {
            NavigationEntry leaving = _screenStack[_screenStack.Count - 1];
            _screenStack.RemoveAt(_screenStack.Count - 1);
            _ui.CloseScreen(leaving.Screen);
            leaving.ResultCallback?.Invoke(result);
        }

        public NavigationResult OpenPopup(UIPopupId id, NavigationRequestOptions options = default)
        {
            if (IsNavigating)
            {
                return NavigationResult.AlreadyActive();
            }

            if (!id.IsValid)
            {
                return NavigationResult.NotFound("Popup id is not valid.");
            }

            if (!_popups.TryGet(id, out UIPopup prefab))
            {
                return NavigationResult.NotFound($"Popup '{id}' is not registered.");
            }

            NavigationGuardResult guardResult = EvaluateGuards(CurrentScreenId, UIScreenId.None, NavigationMode.Push, isBackNavigation: false, out string reason);
            if (guardResult == NavigationGuardResult.Block)
            {
                _events?.Publish(new NavigationBlockedEvent(UIScreenId.None, NavigationResultKind.Blocked, reason));
                return NavigationResult.Block(reason);
            }
            if (guardResult == NavigationGuardResult.Defer)
            {
                return NavigationResult.Deferred(reason);
            }

            IsNavigating = true;
            PushTransitionInputBlock();

            UIPopup instance = _ui.OpenPopup(prefab, popup => ApplyParameters(popup, options.Parameters));

            var entry = new PopupEntry { Id = id, Popup = instance, ResultCallback = options.ResultCallback };
            if (options.PausesGameplay && _gameFlow != null)
            {
                entry.PauseToken = _gameFlow.PauseGameplay(options.PauseReason ?? $"UI.Popup:{id}");
            }
            _popupStack.Add(entry);

            _events?.Publish(new PopupOpenedEvent(id));
            FinishNavigation(instance);

            return NavigationResult.Ok();
        }

        public NavigationResult CloseTopPopup(object result = null)
        {
            if (IsNavigating)
            {
                return NavigationResult.AlreadyActive();
            }

            if (_popupStack.Count == 0)
            {
                return NavigationResult.NotFound("No popup is currently open.");
            }

            IsNavigating = true;
            UIPopupResult uiResult = result != null ? UIPopupResult.Confirmed : UIPopupResult.None;
            NavigationResult navResult = ClosePopupEntry(_popupStack.Count - 1, result, uiResult);
            IsNavigating = false;

            return navResult;
        }

        private NavigationResult ClosePopupEntry(int index, object result, UIPopupResult uiResult)
        {
            PopupEntry entry = _popupStack[index];
            _popupStack.RemoveAt(index);

            entry.PauseToken?.Release();
            _ui.ClosePopup(entry.Popup, uiResult);
            entry.ResultCallback?.Invoke(result);

            _events?.Publish(new PopupClosedEvent(entry.Id, uiResult));

            return NavigationResult.Ok();
        }

        public void AddGuard(INavigationGuard guard)
        {
            if (guard != null && !_guards.Contains(guard))
            {
                _guards.Add(guard);
            }
        }

        public void RemoveGuard(INavigationGuard guard) => _guards.Remove(guard);

        private NavigationGuardResult EvaluateGuards(UIScreenId from, UIScreenId to, NavigationMode mode, bool isBackNavigation, out string reason)
        {
            reason = null;
            var context = new NavigationGuardContext(from, to, mode, isBackNavigation);

            for (int i = 0; i < _guards.Count; i++)
            {
                NavigationGuardResult result = _guards[i].Evaluate(in context, out string guardReason);
                if (result != NavigationGuardResult.Allow)
                {
                    reason = guardReason;
                    return result;
                }
            }

            return NavigationGuardResult.Allow;
        }

        public void RegisterEventMapping<TEvent>(UIPopupId id, Func<TEvent, NavigationRequestOptions> optionsFactory = null)
        {
            if (_events == null)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "RegisterEventMapping called with no IEventService registered; ignored.");
                return;
            }

            Type eventType = typeof(TEvent);
            if (_eventMappingUnsubscribe.ContainsKey(eventType))
            {
                throw new InvalidOperationException($"Event type '{eventType.Name}' already has a registered navigation mapping.");
            }

            void Handler(TEvent evt)
            {
                NavigationRequestOptions options = optionsFactory != null ? optionsFactory(evt) : default;
                OpenPopup(id, options);
            }

            _events.Subscribe<TEvent>(Handler);
            _eventMappingUnsubscribe[eventType] = () => _events.Unsubscribe<TEvent>(Handler);
        }

        public void UnregisterEventMapping<TEvent>()
        {
            Type eventType = typeof(TEvent);
            if (_eventMappingUnsubscribe.TryGetValue(eventType, out Action unsubscribe))
            {
                unsubscribe();
                _eventMappingUnsubscribe.Remove(eventType);
            }
        }

        public NavigationDiagnosticsSnapshot GetDiagnostics()
        {
            var screenIds = new UIScreenId[_screenStack.Count];
            for (int i = 0; i < _screenStack.Count; i++)
            {
                screenIds[i] = _screenStack[i].Id;
            }

            var popupIds = new UIPopupId[_popupStack.Count];
            for (int i = 0; i < _popupStack.Count; i++)
            {
                popupIds[i] = _popupStack[i].Id;
            }

            return new NavigationDiagnosticsSnapshot(CurrentScreenId, screenIds, popupIds, IsNavigating, _screens.Count, _popups.Count);
        }

        private static void ApplyParameters(object target, object parameters)
        {
            if (parameters == null)
            {
                return;
            }

            (target as IUINavigationParameterReceiver)?.OnNavigationParameters(parameters);
        }

        /// <summary>Keeps <see cref="IsNavigating"/> true until <paramref name="entered"/>'s
        /// <see cref="IUINavigationTransitionHandler"/> (if any) finishes playing - see
        /// <see cref="IUINavigationTransitionHandler"/>'s remarks on why this is enter-only.</summary>
        private void FinishNavigation(object entered)
        {
            if (entered is IUINavigationTransitionHandler handler && _coroutineRunner != null)
            {
                _coroutineRunner.StartCoroutine(RunEnterTransition(handler.PlayEnterTransition()));
            }
            else
            {
                IsNavigating = false;
                PopTransitionInputBlock();
            }
        }

        private IEnumerator RunEnterTransition(IEnumerator routine)
        {
            yield return routine;
            IsNavigating = false;
            PopTransitionInputBlock();
        }

        private void PushTransitionInputBlock()
        {
            if (_input == null || _transitionContextPushed)
            {
                return;
            }

            _input.PushContext(InputContextDefinition.Restricted(TransitionInputContextName));
            _transitionContextPushed = true;
        }

        private void PopTransitionInputBlock()
        {
            if (_input == null || !_transitionContextPushed)
            {
                return;
            }

            _input.PopContext();
            _transitionContextPushed = false;
        }
    }
}
