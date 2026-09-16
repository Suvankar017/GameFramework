using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GameFramework.Input
{
    /// <summary>
    /// Default <see cref="IInputSampler"/>. Supports both the legacy <see cref="UnityEngine.Input"/>
    /// manager and the New Input System package side by side, matching whichever backend(s)
    /// Project Settings &gt; Player &gt; Active Input Handling actually enables: legacy members are
    /// compiled behind <c>ENABLE_LEGACY_INPUT_MANAGER</c> and New Input System members behind
    /// <c>ENABLE_INPUT_SYSTEM</c> — the same two scripting defines Unity itself sets for that
    /// setting — so this class degrades to a safe "no input" no-op for whichever backend isn't
    /// active, rather than throwing. With Active Input Handling set to "Both" (this project's
    /// current setting), every member below is live simultaneously.
    /// </summary>
    public sealed class UnityInputSampler : IInputSampler
    {
        public bool GetKey(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKey(key);
#else
            return false;
#endif
        }

        public bool GetMouseButton(int button)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButton(button);
#else
            return false;
#endif
        }

        public Vector2 MousePosition
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return UnityEngine.Input.mousePosition;
#else
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#endif
            }
        }

        public float GetAxisRaw(string axisName)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
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
#else
            return 0f;
#endif
        }

        public int TouchCount
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return UnityEngine.Input.touchCount;
#else
                return 0;
#endif
            }
        }

        public RawTouch GetTouch(int index)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Touch touch = UnityEngine.Input.GetTouch(index);
            return new RawTouch(touch.fingerId, touch.position, touch.phase);
#else
            return default;
#endif
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

        public bool GetKey(Key key)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current[key].isPressed;
#else
            return false;
#endif
        }

        public bool GetMouseButton(NewInputMouseButton button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
            {
                return false;
            }

            switch (button)
            {
                case NewInputMouseButton.Left: return Mouse.current.leftButton.isPressed;
                case NewInputMouseButton.Right: return Mouse.current.rightButton.isPressed;
                case NewInputMouseButton.Middle: return Mouse.current.middleButton.isPressed;
                default: return false;
            }
#else
            return false;
#endif
        }

        public bool GetGamepadButton(GamepadButton button)
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null && Gamepad.current[button].isPressed;
#else
            return false;
#endif
        }

        public float GetGamepadTrigger(bool rightTrigger)
        {
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current == null)
            {
                return 0f;
            }

            return rightTrigger ? Gamepad.current.rightTrigger.ReadValue() : Gamepad.current.leftTrigger.ReadValue();
#else
            return 0f;
#endif
        }

        public Vector2 GetGamepadStick(GamepadStickSource source)
        {
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current == null)
            {
                return Vector2.zero;
            }

            switch (source)
            {
                case GamepadStickSource.LeftStick: return Gamepad.current.leftStick.ReadValue();
                case GamepadStickSource.RightStick: return Gamepad.current.rightStick.ReadValue();
                case GamepadStickSource.DPad: return Gamepad.current.dpad.ReadValue();
                default: return Vector2.zero;
            }
#else
            return Vector2.zero;
#endif
        }
    }
}
