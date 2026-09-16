using System;
using GameFramework.Input;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GameFramework.Input.Tests
{
    public class InputServiceTests
    {
        private FakeInputSampler _sampler;
        private InputService _input;

        [SetUp]
        public void SetUp()
        {
            _sampler = new FakeInputSampler();
            _input = new InputService(_sampler);
            _input.Initialize(new ServiceRegistry());
        }

        private static InputActionMapAsset BuildMap(params InputActionBindingDefinition[] actions)
        {
            var map = ScriptableObject.CreateInstance<InputActionMapAsset>();
            map.Actions.AddRange(actions);
            return map;
        }

        [Test]
        public void Button_PressedThenHeldThenReleased_ReportsCorrectEdges()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Jump",
                Type = InputActionType.Button,
                KeyboardKeys = new[] { KeyCode.Space }
            });
            _input.RegisterActionMap(map);

            _sampler.SetKeyDown(KeyCode.Space, true);
            _input.Tick();
            Assert.IsTrue(_input.GetButtonDown("Jump"));
            Assert.IsTrue(_input.GetButtonHeld("Jump"));
            Assert.IsFalse(_input.GetButtonUp("Jump"));

            _input.Tick(); // still held, second frame
            Assert.IsFalse(_input.GetButtonDown("Jump"));
            Assert.IsTrue(_input.GetButtonHeld("Jump"));

            _sampler.SetKeyDown(KeyCode.Space, false);
            _input.Tick();
            Assert.IsFalse(_input.GetButtonHeld("Jump"));
            Assert.IsTrue(_input.GetButtonUp("Jump"));
        }

        [Test]
        public void Button_MouseBinding_IsRecognized()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Fire",
                Type = InputActionType.Button,
                MouseButtons = new[] { 0 }
            });
            _input.RegisterActionMap(map);

            _sampler.SetMouseButtonDown(0, true);
            _input.Tick();

            Assert.IsTrue(_input.GetButtonDown("Fire"));
        }

        [Test]
        public void Axis_ReadsUnderlyingLegacyAxis()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Throttle",
                Type = InputActionType.Axis,
                LegacyAxisName = "Vertical"
            });
            _input.RegisterActionMap(map);

            _sampler.SetAxis("Vertical", 0.75f);
            _input.Tick();

            Assert.AreEqual(0.75f, _input.GetAxis("Throttle"), 0.0001f);
        }

        [Test]
        public void Vector2_CombinesTwoLegacyAxes()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Move",
                Type = InputActionType.Vector2,
                LegacyAxisNameX = "Horizontal",
                LegacyAxisNameY = "Vertical"
            });
            _input.RegisterActionMap(map);

            _sampler.SetAxis("Horizontal", -1f);
            _sampler.SetAxis("Vertical", 0.5f);
            _input.Tick();

            Assert.AreEqual(new Vector2(-1f, 0.5f), _input.GetVector2("Move"));
        }

        [Test]
        public void Button_NewInputKeyBinding_IsRecognized()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Jump",
                Type = InputActionType.Button,
                NewInputKeyboardKeys = new[] { Key.Space }
            });
            _input.RegisterActionMap(map);

            _sampler.SetKeyDown(Key.Space, true);
            _input.Tick();

            Assert.IsTrue(_input.GetButtonDown("Jump"));
        }

        [Test]
        public void Button_GamepadButtonBinding_IsRecognized()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Jump",
                Type = InputActionType.Button,
                GamepadButtons = new[] { GamepadButton.South }
            });
            _input.RegisterActionMap(map);

            _sampler.SetGamepadButtonDown(GamepadButton.South, true);
            _input.Tick();

            Assert.IsTrue(_input.GetButtonDown("Jump"));
        }

        [Test]
        public void Button_LegacyAndNewInputSources_AreBothHonored()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Jump",
                Type = InputActionType.Button,
                KeyboardKeys = new[] { KeyCode.Space },
                GamepadButtons = new[] { GamepadButton.South }
            });
            _input.RegisterActionMap(map);

            // Legacy source alone satisfies the action.
            _sampler.SetKeyDown(KeyCode.Space, true);
            _input.Tick();
            Assert.IsTrue(_input.GetButtonHeld("Jump"));

            _sampler.SetKeyDown(KeyCode.Space, false);
            _input.Tick();
            Assert.IsFalse(_input.GetButtonHeld("Jump"));

            // New Input System source alone also satisfies the same action.
            _sampler.SetGamepadButtonDown(GamepadButton.South, true);
            _input.Tick();
            Assert.IsTrue(_input.GetButtonHeld("Jump"));
        }

        [Test]
        public void Axis_GamepadTrigger_UsedWhenLargerThanLegacyAxis()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Throttle",
                Type = InputActionType.Axis,
                LegacyAxisName = "Vertical",
                GamepadAxis = GamepadAxisSource.RightTrigger
            });
            _input.RegisterActionMap(map);

            _sampler.SetAxis("Vertical", 0.1f);
            _sampler.SetGamepadTrigger(rightTrigger: true, value: 0.9f);
            _input.Tick();

            Assert.AreEqual(0.9f, _input.GetAxis("Throttle"), 0.0001f);
        }

        [Test]
        public void Vector2_GamepadStick_UsedWhenLargerThanLegacyAxes()
        {
            var map = BuildMap(new InputActionBindingDefinition
            {
                ActionName = "Move",
                Type = InputActionType.Vector2,
                LegacyAxisNameX = "Horizontal",
                LegacyAxisNameY = "Vertical",
                GamepadStick = GamepadStickSource.LeftStick
            });
            _input.RegisterActionMap(map);

            _sampler.SetAxis("Horizontal", 0.05f);
            _sampler.SetAxis("Vertical", 0f);
            _sampler.SetGamepadStick(GamepadStickSource.LeftStick, new Vector2(-1f, 0.5f));
            _input.Tick();

            Assert.AreEqual(new Vector2(-1f, 0.5f), _input.GetVector2("Move"));
        }

        [Test]
        public void GetActionState_UnregisteredAction_ReturnsNone()
        {
            Assert.AreEqual(InputActionState.None.IsPressed, _input.GetActionState("Missing").IsPressed);
        }

        [Test]
        public void RegisterActionMap_DuplicateActionName_Throws()
        {
            _input.RegisterActionMap(BuildMap(new InputActionBindingDefinition { ActionName = "Jump", Type = InputActionType.Button }));

            Assert.Throws<InvalidOperationException>(() =>
                _input.RegisterActionMap(BuildMap(new InputActionBindingDefinition { ActionName = "Jump", Type = InputActionType.Button })));
        }

        [Test]
        public void UnregisterActionMap_RemovesItsActions()
        {
            var map = BuildMap(new InputActionBindingDefinition { ActionName = "Jump", Type = InputActionType.Button, KeyboardKeys = new[] { KeyCode.Space } });
            _input.RegisterActionMap(map);

            _input.UnregisterActionMap(map);

            _sampler.SetKeyDown(KeyCode.Space, true);
            _input.Tick();
            Assert.IsFalse(_input.GetButtonDown("Jump"));
        }

        [Test]
        public void DefaultContext_AllowsAllActions()
        {
            Assert.IsTrue(_input.IsActionEnabled("AnythingAtAll"));
            Assert.AreEqual("Gameplay", _input.CurrentContextName);
        }

        [Test]
        public void PushRestrictedContext_DisablesActionsNotListed()
        {
            var map = BuildMap(new InputActionBindingDefinition { ActionName = "Jump", Type = InputActionType.Button, KeyboardKeys = new[] { KeyCode.Space } });
            _input.RegisterActionMap(map);
            _sampler.SetKeyDown(KeyCode.Space, true);

            _input.PushContext(InputContextDefinition.Restricted("Popup", "Cancel"));
            _input.Tick();

            Assert.IsFalse(_input.IsActionEnabled("Jump"));
            Assert.IsFalse(_input.GetButtonDown("Jump")); // suppressed even though physically pressed
        }

        [Test]
        public void PopContext_RestoresPreviousContext()
        {
            _input.PushContext(InputContextDefinition.Restricted("Popup", "Cancel"));
            _input.PopContext();

            Assert.AreEqual("Gameplay", _input.CurrentContextName);
            Assert.IsTrue(_input.IsActionEnabled("Jump"));
        }

        [Test]
        public void PopContext_WithOnlyBaseContext_DoesNotThrowAndKeepsBaseContext()
        {
            Assert.DoesNotThrow(() => _input.PopContext());
            Assert.AreEqual("Gameplay", _input.CurrentContextName);
        }

        [Test]
        public void Pointer_WithNoTouches_ReflectsMouse()
        {
            _sampler.MousePosition = new Vector2(100f, 200f);
            _sampler.SetMouseButtonDown(0, true);

            _input.Tick();

            Assert.AreEqual(-1, _input.Pointer.PointerId);
            Assert.AreEqual(new Vector2(100f, 200f), _input.Pointer.ScreenPosition);
            Assert.IsTrue(_input.Pointer.IsDown);
            Assert.IsTrue(_input.Pointer.WasPressedThisFrame);
        }

        [Test]
        public void Pointer_WithActiveTouch_PrefersTouchOverMouse()
        {
            _sampler.SetTouches(new RawTouch(0, new Vector2(5f, 6f), UnityEngine.TouchPhase.Began));

            _input.Tick();

            Assert.AreEqual(0, _input.Pointer.PointerId);
            Assert.AreEqual(1, _input.ActiveTouches.Count);
            Assert.IsTrue(_input.Pointer.WasPressedThisFrame);
        }

        [Test]
        public void Shutdown_ClearsRegisteredActions()
        {
            _input.RegisterActionMap(BuildMap(new InputActionBindingDefinition { ActionName = "Jump", Type = InputActionType.Button }));

            _input.Shutdown();

            Assert.AreEqual(InputActionState.None.IsPressed, _input.GetActionState("Jump").IsPressed);
        }
    }
}
