using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;

namespace GameFramework.Gameplay.Objectives
{
    /// <summary>
    /// Reusable objective state machine: Inactive → Active → (Completed | Failed) → Inactive (via
    /// <see cref="Reset"/>). Invalid transitions are rejected with a logged warning, never an
    /// exception or a silent state change. A game defines what the objective actually checks by
    /// subclassing this and calling <see cref="Complete"/>/<see cref="Fail"/> itself; this base
    /// class only owns the state machine and event publication.
    /// </summary>
    public abstract class ObjectiveBase : IObjective
    {
        private const string LogCategory = "Objectives";

        /// <summary>May be null — a null service means state transitions still work, but no
        /// events are published, which keeps this usable in fully isolated unit tests.</summary>
        protected readonly IEventService Events;

        protected ObjectiveBase(string id, IEventService events = null)
        {
            Id = Guard.NotNullOrEmpty(id, nameof(id));
            Events = events;
        }

        public string Id { get; }
        public ObjectiveState State { get; private set; } = ObjectiveState.Inactive;

        public void Activate()
        {
            if (State != ObjectiveState.Inactive)
            {
                LogInvalidTransition(nameof(Activate));
                return;
            }

            State = ObjectiveState.Active;
            OnActivated();
            Events?.Publish(new ObjectiveActivatedEvent(Id));
        }

        public void Complete()
        {
            if (State != ObjectiveState.Active)
            {
                LogInvalidTransition(nameof(Complete));
                return;
            }

            State = ObjectiveState.Completed;
            OnCompleted();
            Events?.Publish(new ObjectiveCompletedEvent(Id));
        }

        public void Fail()
        {
            if (State != ObjectiveState.Active)
            {
                LogInvalidTransition(nameof(Fail));
                return;
            }

            State = ObjectiveState.Failed;
            OnFailed();
            Events?.Publish(new ObjectiveFailedEvent(Id));
        }

        public void Reset()
        {
            if (State != ObjectiveState.Completed && State != ObjectiveState.Failed)
            {
                LogInvalidTransition(nameof(Reset));
                return;
            }

            State = ObjectiveState.Inactive;
            OnReset();
        }

        /// <summary>Called after the state has already changed to Active.</summary>
        protected virtual void OnActivated()
        {
        }

        /// <summary>Called after the state has already changed to Completed.</summary>
        protected virtual void OnCompleted()
        {
        }

        /// <summary>Called after the state has already changed to Failed.</summary>
        protected virtual void OnFailed()
        {
        }

        /// <summary>Called after the state has already changed back to Inactive.</summary>
        protected virtual void OnReset()
        {
        }

        private void LogInvalidTransition(string attemptedTransition)
        {
            Log.Warning(LogCategory, $"Objective '{Id}' cannot {attemptedTransition} from state {State}; ignored.");
        }
    }
}
