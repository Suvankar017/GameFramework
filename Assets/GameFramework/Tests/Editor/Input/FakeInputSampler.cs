using System.Collections.Generic;
using GameFramework.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GameFramework.Input.Tests
{
    /// <summary>Deterministic <see cref="IInputSampler"/> for tests — every value is set directly
    /// rather than read from a live device.</summary>
    internal sealed class FakeInputSampler : IInputSampler
    {
        private readonly HashSet<KeyCode> _pressedKeys = new HashSet<KeyCode>();
        private readonly HashSet<int> _pressedMouseButtons = new HashSet<int>();
        private readonly Dictionary<string, float> _axes = new Dictionary<string, float>();
        private readonly List<RawTouch> _touches = new List<RawTouch>();
        private readonly HashSet<Key> _pressedNewInputKeys = new HashSet<Key>();
        private readonly HashSet<NewInputMouseButton> _pressedNewInputMouseButtons = new HashSet<NewInputMouseButton>();
        private readonly HashSet<GamepadButton> _pressedGamepadButtons = new HashSet<GamepadButton>();
        private readonly Dictionary<GamepadStickSource, Vector2> _gamepadSticks = new Dictionary<GamepadStickSource, Vector2>();
        private float _leftTrigger;
        private float _rightTrigger;

        public Vector2 MousePosition { get; set; }
        public bool PointerOverUI { get; set; }

        public void SetKeyDown(KeyCode key, bool down)
        {
            if (down) _pressedKeys.Add(key); else _pressedKeys.Remove(key);
        }

        public void SetMouseButtonDown(int button, bool down)
        {
            if (down) _pressedMouseButtons.Add(button); else _pressedMouseButtons.Remove(button);
        }

        public void SetAxis(string axisName, float value) => _axes[axisName] = value;

        public void SetTouches(params RawTouch[] touches)
        {
            _touches.Clear();
            _touches.AddRange(touches);
        }

        public void SetKeyDown(Key key, bool down)
        {
            if (down) _pressedNewInputKeys.Add(key); else _pressedNewInputKeys.Remove(key);
        }

        public void SetMouseButtonDown(NewInputMouseButton button, bool down)
        {
            if (down) _pressedNewInputMouseButtons.Add(button); else _pressedNewInputMouseButtons.Remove(button);
        }

        public void SetGamepadButtonDown(GamepadButton button, bool down)
        {
            if (down) _pressedGamepadButtons.Add(button); else _pressedGamepadButtons.Remove(button);
        }

        public void SetGamepadTrigger(bool rightTrigger, float value)
        {
            if (rightTrigger) _rightTrigger = value; else _leftTrigger = value;
        }

        public void SetGamepadStick(GamepadStickSource source, Vector2 value) => _gamepadSticks[source] = value;

        public bool GetKey(KeyCode key) => _pressedKeys.Contains(key);
        public bool GetMouseButton(int button) => _pressedMouseButtons.Contains(button);
        public float GetAxisRaw(string axisName) => _axes.TryGetValue(axisName, out float value) ? value : 0f;
        public int TouchCount => _touches.Count;
        public RawTouch GetTouch(int index) => _touches[index];
        public bool IsPointerOverUI(int pointerId) => PointerOverUI;

        public bool GetKey(Key key) => _pressedNewInputKeys.Contains(key);
        public bool GetMouseButton(NewInputMouseButton button) => _pressedNewInputMouseButtons.Contains(button);
        public bool GetGamepadButton(GamepadButton button) => _pressedGamepadButtons.Contains(button);
        public float GetGamepadTrigger(bool rightTrigger) => rightTrigger ? _rightTrigger : _leftTrigger;

        public Vector2 GetGamepadStick(GamepadStickSource source) =>
            _gamepadSticks.TryGetValue(source, out Vector2 value) ? value : Vector2.zero;
    }
}
