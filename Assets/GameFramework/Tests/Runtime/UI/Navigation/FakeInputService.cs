using System.Collections.Generic;
using GameFramework.Input;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.UI.Navigation.Tests
{
    /// <summary>Minimal test double for <see cref="IInputService"/> - only tracks
    /// <see cref="PushContext"/>/<see cref="PopContext"/> calls, which is all
    /// <see cref="NavigationService"/> actually exercises directly (its back-button driver's own
    /// <c>GetButtonDown</c> read isn't tested here - see the test file's remarks on why key-press
    /// simulation is out of scope).</summary>
    internal sealed class FakeInputService : IInputService
    {
        public int PushContextCount;
        public int PopContextCount;
        public readonly List<string> PushedContextNames = new List<string>();

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

        public InputActionState GetActionState(string actionName) => InputActionState.None;
        public bool GetButtonDown(string actionName) => false;
        public bool GetButtonHeld(string actionName) => false;
        public bool GetButtonUp(string actionName) => false;
        public float GetAxis(string actionName) => 0f;
        public Vector2 GetVector2(string actionName) => Vector2.zero;

        public PointerState Pointer => PointerState.None;
        public IReadOnlyList<PointerState> ActiveTouches { get; } = new List<PointerState>();

        public void PushContext(InputContextDefinition context)
        {
            PushContextCount++;
            PushedContextNames.Add(context.Name);
        }

        public void PopContext()
        {
            PopContextCount++;
            if (PushedContextNames.Count > 0)
            {
                PushedContextNames.RemoveAt(PushedContextNames.Count - 1);
            }
        }

        public string CurrentContextName => PushedContextNames.Count > 0 ? PushedContextNames[PushedContextNames.Count - 1] : null;
        public bool IsActionEnabled(string actionName) => true;
    }
}
