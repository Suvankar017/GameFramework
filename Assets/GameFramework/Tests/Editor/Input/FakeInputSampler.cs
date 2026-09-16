using System.Collections.Generic;
using GameFramework.Input;
using UnityEngine;

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

        public bool GetKey(KeyCode key) => _pressedKeys.Contains(key);
        public bool GetMouseButton(int button) => _pressedMouseButtons.Contains(button);
        public float GetAxisRaw(string axisName) => _axes.TryGetValue(axisName, out float value) ? value : 0f;
        public int TouchCount => _touches.Count;
        public RawTouch GetTouch(int index) => _touches[index];
        public bool IsPointerOverUI(int pointerId) => PointerOverUI;
    }
}
