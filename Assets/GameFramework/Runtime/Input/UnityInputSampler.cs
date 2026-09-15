using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFramework.Input
{
    /// <summary>
    /// Default <see cref="IInputSampler"/> backed by the legacy <see cref="UnityEngine.Input"/>
    /// manager. Using the legacy manager (rather than the newer Input System package, which this
    /// project does not have installed) means default axes such as "Horizontal"/"Vertical"/"Fire1"
    /// already blend keyboard and joystick input with no extra Project Settings changes required.
    /// </summary>
    public sealed class UnityInputSampler : IInputSampler
    {
        public bool GetKey(KeyCode key) => UnityEngine.Input.GetKey(key);

        public bool GetMouseButton(int button) => UnityEngine.Input.GetMouseButton(button);

        public Vector2 MousePosition => UnityEngine.Input.mousePosition;

        public float GetAxisRaw(string axisName)
        {
            if (string.IsNullOrEmpty(axisName))
            {
                return 0f;
            }

            try
            {
                return UnityEngine.Input.GetAxisRaw(axisName);
            }
            catch (System.Exception)
            {
                // Thrown by the Input Manager when the named axis isn't configured in Project
                // Settings. Treated as "no input" rather than a crash — a missing axis is a
                // configuration issue to fix in the inspector, not a runtime failure.
                return 0f;
            }
        }

        public int TouchCount => UnityEngine.Input.touchCount;

        public RawTouch GetTouch(int index)
        {
            Touch touch = UnityEngine.Input.GetTouch(index);
            return new RawTouch(touch.fingerId, touch.position, touch.phase);
        }

        public bool IsPointerOverUI(int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            return pointerId < 0 ? eventSystem.IsPointerOverGameObject() : eventSystem.IsPointerOverGameObject(pointerId);
        }
    }
}
