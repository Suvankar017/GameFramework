using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// Default <see cref="IInputService"/>. Samples every registered action once per
    /// <see cref="Tick"/> (driven by <see cref="Bootstrap.GameBootstrapper"/> via
    /// <see cref="IUpdatableService"/>, exactly like <c>TimerService</c>) and caches the result;
    /// <see cref="GetActionState"/> and friends are pure cache reads with no device query.
    /// </summary>
    public sealed class InputService : IInputService, IUpdatableService
    {
        private sealed class ActionEntry
        {
            public InputActionBindingDefinition Binding;
            public InputActionState State;
        }

        private readonly Dictionary<string, ActionEntry> _actions = new Dictionary<string, ActionEntry>();
        private readonly Dictionary<InputActionMapAsset, List<string>> _mapActionNames =
            new Dictionary<InputActionMapAsset, List<string>>();
        private readonly List<InputContextDefinition> _contextStack = new List<InputContextDefinition>();
        private readonly List<PointerState> _activeTouches = new List<PointerState>();
        private readonly IInputSampler _sampler;
        private ILoggingService _log;
        private PointerState _mousePointerLastFrame = PointerState.None;

        private const string DefaultContextName = "Gameplay";
        private const string LogCategory = "Input";

        public InputService() : this(new UnityInputSampler())
        {
        }

        internal InputService(IInputSampler sampler)
        {
            _sampler = Guard.NotNull(sampler, nameof(sampler));
        }

        public PointerState Pointer { get; private set; } = PointerState.None;
        public IReadOnlyList<PointerState> ActiveTouches => _activeTouches;
        public string CurrentContextName => _contextStack.Count > 0 ? _contextStack[_contextStack.Count - 1].Name : null;

        public void Initialize(IServiceRegistry registry)
        {
            registry.TryGet(out _log);
            _contextStack.Add(InputContextDefinition.AllowAll(DefaultContextName));
        }

        public void Shutdown()
        {
            _actions.Clear();
            _mapActionNames.Clear();
            _contextStack.Clear();
            _activeTouches.Clear();
        }

        public void RegisterActionMap(InputActionMapAsset map)
        {
            Guard.NotNull(map, nameof(map));

            var registeredNames = new List<string>();
            foreach (InputActionBindingDefinition binding in map.Actions)
            {
                if (string.IsNullOrEmpty(binding.ActionName))
                {
                    continue;
                }

                if (_actions.ContainsKey(binding.ActionName))
                {
                    throw new InvalidOperationException(
                        $"Input action '{binding.ActionName}' is already registered by another map.");
                }

                _actions.Add(binding.ActionName, new ActionEntry { Binding = binding, State = InputActionState.None });
                registeredNames.Add(binding.ActionName);
            }

            _mapActionNames[map] = registeredNames;
        }

        public void UnregisterActionMap(InputActionMapAsset map)
        {
            if (map == null || !_mapActionNames.TryGetValue(map, out List<string> names))
            {
                return;
            }

            foreach (string name in names)
            {
                _actions.Remove(name);
            }

            _mapActionNames.Remove(map);
        }

        public InputActionState GetActionState(string actionName)
        {
            if (!IsActionEnabled(actionName))
            {
                return InputActionState.None;
            }

            return _actions.TryGetValue(actionName, out ActionEntry entry) ? entry.State : InputActionState.None;
        }

        public bool GetButtonDown(string actionName) => GetActionState(actionName).WasPressedThisFrame;
        public bool GetButtonHeld(string actionName) => GetActionState(actionName).IsPressed;
        public bool GetButtonUp(string actionName) => GetActionState(actionName).WasReleasedThisFrame;
        public float GetAxis(string actionName) => GetActionState(actionName).AxisValue;
        public Vector2 GetVector2(string actionName) => GetActionState(actionName).Vector2Value;

        public void PushContext(InputContextDefinition context)
        {
            _contextStack.Add(context);
        }

        public void PopContext()
        {
            if (_contextStack.Count <= 1)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "PopContext called with only the base context remaining; ignored.");
                return;
            }

            _contextStack.RemoveAt(_contextStack.Count - 1);
        }

        public bool IsActionEnabled(string actionName)
        {
            if (_contextStack.Count == 0)
            {
                return true;
            }

            return _contextStack[_contextStack.Count - 1].Allows(actionName);
        }

        public void Tick()
        {
            foreach (ActionEntry entry in _actions.Values)
            {
                entry.State = Sample(entry.Binding, entry.State);
            }

            SamplePointers();
        }

        private InputActionState Sample(InputActionBindingDefinition binding, InputActionState previous)
        {
            switch (binding.Type)
            {
                case InputActionType.Button:
                    return SampleButton(binding, previous);
                case InputActionType.Axis:
                    float axis = _sampler.GetAxisRaw(binding.LegacyAxisName);
                    return new InputActionState(false, false, false, axis, Vector2.zero);
                case InputActionType.Vector2:
                    float x = _sampler.GetAxisRaw(binding.LegacyAxisNameX);
                    float y = _sampler.GetAxisRaw(binding.LegacyAxisNameY);
                    return new InputActionState(false, false, false, 0f, new Vector2(x, y));
                default:
                    return InputActionState.None;
            }
        }

        private InputActionState SampleButton(InputActionBindingDefinition binding, InputActionState previous)
        {
            bool isPressed = false;

            for (int i = 0; i < binding.KeyboardKeys.Length && !isPressed; i++)
            {
                isPressed = _sampler.GetKey(binding.KeyboardKeys[i]);
            }

            for (int i = 0; i < binding.MouseButtons.Length && !isPressed; i++)
            {
                isPressed = _sampler.GetMouseButton(binding.MouseButtons[i]);
            }

            bool wasPressed = previous.IsPressed;
            return new InputActionState(
                isPressed,
                wasPressedThisFrame: isPressed && !wasPressed,
                wasReleasedThisFrame: !isPressed && wasPressed,
                axisValue: isPressed ? 1f : 0f,
                vector2Value: Vector2.zero);
        }

        private void SamplePointers()
        {
            _activeTouches.Clear();

            int touchCount = _sampler.TouchCount;
            for (int i = 0; i < touchCount; i++)
            {
                RawTouch touch = _sampler.GetTouch(i);
                bool isDown = touch.Phase != TouchPhase.Ended && touch.Phase != TouchPhase.Canceled;
                _activeTouches.Add(new PointerState(
                    touch.FingerId,
                    touch.Position,
                    isDown,
                    wasPressedThisFrame: touch.Phase == TouchPhase.Began,
                    wasReleasedThisFrame: touch.Phase == TouchPhase.Ended || touch.Phase == TouchPhase.Canceled,
                    isOverUI: _sampler.IsPointerOverUI(touch.FingerId)));
            }

            if (_activeTouches.Count > 0)
            {
                Pointer = _activeTouches[0];
                return;
            }

            bool mouseDown = _sampler.GetMouseButton(0);
            bool wasMouseDown = _mousePointerLastFrame.IsDown;
            var mousePointer = new PointerState(
                -1,
                _sampler.MousePosition,
                mouseDown,
                wasPressedThisFrame: mouseDown && !wasMouseDown,
                wasReleasedThisFrame: !mouseDown && wasMouseDown,
                isOverUI: _sampler.IsPointerOverUI(-1));

            Pointer = mousePointer;
            _mousePointerLastFrame = mousePointer;
        }
    }
}
