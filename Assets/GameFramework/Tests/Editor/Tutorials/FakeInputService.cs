using System.Collections.Generic;
using GameFramework.Input;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>Minimal test double for <see cref="IInputService"/> - a settable action-state table
    /// plus a real context stack (so <see cref="InputStep"/> and tutorial-level input-gating tests
    /// exercise the same gating logic the real <c>InputService</c> enforces).</summary>
    internal sealed class FakeInputService : IInputService
    {
        private readonly Dictionary<string, InputActionState> _states = new Dictionary<string, InputActionState>();
        private readonly List<InputContextDefinition> _contextStack = new List<InputContextDefinition>
        {
            InputContextDefinition.AllowAll("Gameplay")
        };

        public int PushContextCallCount { get; private set; }
        public int PopContextCallCount { get; private set; }

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void RegisterActionMap(InputActionMapAsset map)
        {
        }

        public void UnregisterActionMap(InputActionMapAsset map)
        {
        }

        public void SetState(string actionName, InputActionState state) => _states[actionName] = state;

        public InputActionState GetActionState(string actionName)
        {
            if (!IsActionEnabled(actionName))
            {
                return InputActionState.None;
            }

            return _states.TryGetValue(actionName, out InputActionState state) ? state : InputActionState.None;
        }

        public bool GetButtonDown(string actionName) => GetActionState(actionName).WasPressedThisFrame;
        public bool GetButtonHeld(string actionName) => GetActionState(actionName).IsPressed;
        public bool GetButtonUp(string actionName) => GetActionState(actionName).WasReleasedThisFrame;
        public float GetAxis(string actionName) => GetActionState(actionName).AxisValue;
        public Vector2 GetVector2(string actionName) => GetActionState(actionName).Vector2Value;

        public PointerState Pointer => PointerState.None;
        public IReadOnlyList<PointerState> ActiveTouches { get; } = new List<PointerState>();

        public void PushContext(InputContextDefinition context)
        {
            _contextStack.Add(context);
            PushContextCallCount++;
        }

        public void PopContext()
        {
            if (_contextStack.Count <= 1)
            {
                return;
            }

            _contextStack.RemoveAt(_contextStack.Count - 1);
            PopContextCallCount++;
        }

        public string CurrentContextName => _contextStack[_contextStack.Count - 1].Name;

        public bool IsActionEnabled(string actionName) => _contextStack[_contextStack.Count - 1].Allows(actionName);
    }
}
