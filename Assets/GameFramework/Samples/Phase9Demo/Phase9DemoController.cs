using System.Collections;
using System.Collections.Generic;
using GameFramework.Input;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials;
using GameFramework.Tutorials.Conditions;
using GameFramework.Tutorials.Steps;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GameFramework.Samples.Phase9Demo
{
    /// <summary>
    /// Drives the Phase 9 demonstration scene: registers and runs a single "FirstDrive" tutorial -
    /// the exact Instruction -> Input -> Event -> Condition -> Instruction sequence used as the
    /// worked example in CLAUDE.md's Phase 9 brief (section 4) - showing every built-in step type
    /// working together, plus pause/skip/cancel/restart and completion persistence. Uses the five
    /// content assets under <c>Content/</c> (the framework's own <see cref="TutorialBootstrapper"/>
    /// registers only the service - see this class for the game-specific step of registering actual
    /// tutorial content, done here after Ready, the same pattern every other phase's demo
    /// controller follows). Not part of the reusable framework - sample/demo content only, kept in
    /// its own assembly and scene.
    /// </summary>
    public sealed class Phase9DemoController : MonoBehaviour
    {
        private static readonly TutorialId FirstDrive = new TutorialId("FirstDrive");

        [Tooltip("Assigned from Content/FirstDriveTutorial.asset.")]
        [SerializeField] private TutorialDefinition _tutorialDefinition;

        [Tooltip("Assigned from Content/Step1_Intro.asset.")]
        [SerializeField] private TutorialStepDefinition _introStep;

        [Tooltip("Assigned from Content/Step2_PressMove.asset.")]
        [SerializeField] private TutorialStepDefinition _pressMoveStep;

        [Tooltip("Assigned from Content/Step3_WaitForMovement.asset.")]
        [SerializeField] private TutorialStepDefinition _waitForMovementStep;

        [Tooltip("Assigned from Content/Step4_ReachCheckpoint.asset.")]
        [SerializeField] private TutorialStepDefinition _reachCheckpointStep;

        [Tooltip("Assigned from Content/Step5_Outro.asset.")]
        [SerializeField] private TutorialStepDefinition _outroStep;

        private IInputService _input;
        private ITutorialService _tutorials;
        private IEventService _events;
        private bool _ready;
        private bool _checkpointReached;

        /// <summary>Game-defined event a real car controller would publish - the Tutorial framework
        /// never knows what this means, it only waits for one (see <see cref="EventStep{TEvent}"/>).</summary>
        private readonly struct CarStartedMovingEvent
        {
        }

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _input = services.Get<IInputService>();
            _events = services.Get<IEventService>();
            _tutorials = services.Get<ITutorialService>();

            _input.RegisterActionMap(BuildDemoInputMap());

            RegisterDemoContent();
            SubscribeToTutorialEvents();

            _ready = true;
            Debug.Log("[Phase9Demo] Ready. Keys: 1 = start FirstDrive, 2 = acknowledge the current instruction, " +
                "Space/Move = satisfies the input step, 3 = report the car started moving (event step), " +
                "4 = reach the checkpoint (condition step), S = skip, C = cancel, R = status report.");
            LogStatus();
        }

        private void RegisterDemoContent()
        {
            var steps = new List<TutorialStepEntry>
            {
                new TutorialStepEntry(_introStep, new InstructionStep(_introStep.Id)),
                new TutorialStepEntry(_pressMoveStep, new InputStep(_pressMoveStep.Id, _input, "Move", InputTriggerType.Pressed)),
                new TutorialStepEntry(_waitForMovementStep, new EventStep<CarStartedMovingEvent>(_waitForMovementStep.Id, _events)),
                new TutorialStepEntry(_reachCheckpointStep, new ConditionStep(_reachCheckpointStep.Id, new DelegateTutorialCondition(() => _checkpointReached, "Player reaches the first checkpoint"))),
                new TutorialStepEntry(_outroStep, new InstructionStep(_outroStep.Id))
            };

            _tutorials.RegisterTutorial(_tutorialDefinition, steps);
        }

        private void SubscribeToTutorialEvents()
        {
            _events.Subscribe<TutorialStartedEvent>(e => Debug.Log($"[Phase9Demo] TutorialStarted: {e.TutorialId}"));
            _events.Subscribe<TutorialStepStartedEvent>(e => Debug.Log($"[Phase9Demo] StepStarted: {e.StepId} ({e.StepIndex + 1}/{e.StepCount})"));
            _events.Subscribe<TutorialStepCompletedEvent>(e => Debug.Log($"[Phase9Demo] StepCompleted: {e.StepId}"));
            _events.Subscribe<TutorialCompletedEvent>(e => Debug.Log($"[Phase9Demo] TutorialCompleted: {e.TutorialId}"));
            _events.Subscribe<TutorialSkippedEvent>(e => Debug.Log($"[Phase9Demo] TutorialSkipped: {e.TutorialId}"));
            _events.Subscribe<TutorialCancelledEvent>(e => Debug.Log($"[Phase9Demo] TutorialCancelled: {e.TutorialId}"));
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                TutorialStartResult result = _tutorials.Start(FirstDrive);
                Debug.Log($"[Phase9Demo] Start(FirstDrive) -> {result}");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                TutorialCommandResult result = _tutorials.CompleteCurrentStep();
                Debug.Log($"[Phase9Demo] CompleteCurrentStep() -> {result}");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                _events.Publish(new CarStartedMovingEvent());
                Debug.Log("[Phase9Demo] Published CarStartedMovingEvent.");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4))
            {
                _checkpointReached = true;
                Debug.Log("[Phase9Demo] Checkpoint reached.");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log($"[Phase9Demo] Skip() -> {_tutorials.Skip()}");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log($"[Phase9Demo] Cancel() -> {_tutorials.Cancel()}");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                LogStatus();
            }
        }

        private void LogStatus()
        {
            Debug.Log($"[Phase9Demo] Status: State={_tutorials.State}, ActiveStep={_tutorials.ActiveStepId}, " +
                $"HasCompleted={_tutorials.HasCompleted(FirstDrive)}, WasSkipped={_tutorials.WasSkipped(FirstDrive)}.");
        }

        private static InputActionMapAsset BuildDemoInputMap()
        {
            var map = ScriptableObject.CreateInstance<InputActionMapAsset>();
            map.Actions.Add(new InputActionBindingDefinition
            {
                ActionName = "Move",
                Type = GameFramework.Input.InputActionType.Button,
                KeyboardKeys = new[] { KeyCode.Space, KeyCode.W },
                NewInputKeyboardKeys = new[] { Key.Space, Key.W },
                GamepadButtons = new[] { GamepadButton.South }
            });
            return map;
        }
    }
}
