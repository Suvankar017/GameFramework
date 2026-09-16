using System.Collections.Generic;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// Framework input abstraction. Game code asks for a logical action ("Jump", "Move") rather
    /// than a physical device: Physical Input → Input Mapping → Logical Action → Game Feature.
    /// State is sampled once per frame (see <see cref="Runtime.Services.IUpdatableService"/>) and
    /// cached — callers never trigger a device query themselves. Each action's
    /// <see cref="InputActionBindingDefinition"/> can bind legacy Input Manager sources, New Input
    /// System sources (keyboard/mouse/gamepad), or both at once — see
    /// <see cref="InputActionBindingDefinition"/> for how the two are merged.
    /// </summary>
    public interface IInputService : IGameService
    {
        /// <summary>Adds a map's actions to the set the service resolves each frame. Throws if any
        /// action name in the map is already registered by a previously added map.</summary>
        void RegisterActionMap(InputActionMapAsset map);

        /// <summary>Removes a previously registered map's actions.</summary>
        void UnregisterActionMap(InputActionMapAsset map);

        /// <summary>
        /// Returns the sampled state, or <see cref="InputActionState.None"/> if the action isn't
        /// allowed by the current context (see <see cref="PushContext"/>) — a popup blocking
        /// "Jump" means every caller reading "Jump" sees no input, with no per-caller
        /// <c>if (popupOpen)</c> check required.
        /// </summary>
        InputActionState GetActionState(string actionName);
        bool GetButtonDown(string actionName);
        bool GetButtonHeld(string actionName);
        bool GetButtonUp(string actionName);
        float GetAxis(string actionName);
        Vector2 GetVector2(string actionName);

        /// <summary>The primary pointer: the first active touch if any, otherwise the mouse.</summary>
        PointerState Pointer { get; }

        /// <summary>Every currently active touch, for multi-touch gameplay. Empty (not the mouse)
        /// when there is no touch input this frame.</summary>
        IReadOnlyList<PointerState> ActiveTouches { get; }

        /// <summary>Pushes a context onto the stack; its allowed-action set is what
        /// <see cref="IsActionEnabled"/> reports until it (or something above it) is popped.</summary>
        void PushContext(InputContextDefinition context);

        /// <summary>Pops the top context. Throws if the stack would become empty — a base
        /// "Gameplay" context is expected to remain for the service's lifetime.</summary>
        void PopContext();

        /// <summary>Name of the currently active (top-of-stack) context.</summary>
        string CurrentContextName { get; }

        /// <summary>True if <paramref name="actionName"/> is allowed by the current top-of-stack
        /// context. Exposed for UI/diagnostics; gameplay code does not need to call this itself
        /// since <see cref="GetActionState"/> and friends already enforce it.</summary>
        bool IsActionEnabled(string actionName);
    }
}
