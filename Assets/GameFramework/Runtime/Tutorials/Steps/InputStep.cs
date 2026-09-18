using GameFramework.Core.Validation;
using GameFramework.Input;

namespace GameFramework.Tutorials.Steps
{
    /// <summary>
    /// Waits for a logical input action from <see cref="IInputService"/> - never a physical
    /// key/button, per CLAUDE.md's Phase 9 brief section 9. Reads the action through the framework's
    /// existing per-frame sampling/context gating; does not sample any device itself and does not
    /// push its own input context (gating what the *rest of the game* can do while a tutorial runs
    /// is a tutorial-level concern - see <see cref="TutorialService"/>'s remarks on input gating).
    /// </summary>
    public sealed class InputStep : TutorialStepBase
    {
        private readonly IInputService _input;
        private readonly string _actionName;
        private readonly InputTriggerType _trigger;

        public InputStep(string id, IInputService input, string actionName, InputTriggerType trigger = InputTriggerType.Pressed)
            : base(id)
        {
            _input = Guard.NotNull(input, nameof(input));
            _actionName = Guard.NotNullOrEmpty(actionName, nameof(actionName));
            _trigger = trigger;
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (State != TutorialStepState.Active)
            {
                return;
            }

            InputActionState state = _input.GetActionState(_actionName);
            bool triggered;
            switch (_trigger)
            {
                case InputTriggerType.Pressed:
                    triggered = state.WasPressedThisFrame;
                    break;
                case InputTriggerType.Released:
                    triggered = state.WasReleasedThisFrame;
                    break;
                case InputTriggerType.Held:
                    triggered = state.IsPressed;
                    break;
                default:
                    triggered = false;
                    break;
            }

            if (triggered)
            {
                Complete();
            }
        }
    }
}
