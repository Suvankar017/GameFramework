using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;

namespace GameFramework.Gameplay.Lifecycle
{
    /// <summary>
    /// Drives an <see cref="IGameplayObjectLifecycle"/> deterministically, guarding against
    /// out-of-order or duplicate calls (e.g. two <see cref="Initialize"/> calls, or
    /// <see cref="Activate"/> without a prior <see cref="Initialize"/>) rather than letting a
    /// caller's mistake silently re-run one-time setup. Composition, not inheritance — hold one of
    /// these as a field in whatever plain class or MonoBehaviour owns the object; nothing here
    /// requires deriving from a framework base type.
    /// </summary>
    public sealed class GameplayObjectLifecycleRunner
    {
        private const string LogCategory = "Lifecycle";

        private readonly IGameplayObjectLifecycle _target;

        public GameplayObjectLifecycleRunner(IGameplayObjectLifecycle target)
        {
            _target = Guard.NotNull(target, nameof(target));
        }

        public GameplayObjectLifecycleState State { get; private set; } = GameplayObjectLifecycleState.Created;

        /// <summary>No-op (logged) if already initialized — deterministic, never re-runs one-time setup.</summary>
        public void Initialize()
        {
            if (State != GameplayObjectLifecycleState.Created)
            {
                Log.Warning(LogCategory, $"Initialize called more than once on '{_target}'; ignored.");
                return;
            }

            _target.Initialize();
            State = GameplayObjectLifecycleState.Initialized;
        }

        /// <summary>Valid from <see cref="GameplayObjectLifecycleState.Initialized"/> (first use) or
        /// <see cref="GameplayObjectLifecycleState.Deactivated"/> (pool reuse). No-op (logged)
        /// otherwise — in particular, Activate before Initialize is rejected rather than silently
        /// allowed.</summary>
        public void Activate()
        {
            if (State != GameplayObjectLifecycleState.Initialized && State != GameplayObjectLifecycleState.Deactivated)
            {
                Log.Warning(LogCategory, $"Activate called from invalid state '{State}' on '{_target}'; ignored.");
                return;
            }

            _target.Activate();
            State = GameplayObjectLifecycleState.Active;
        }

        /// <summary>No-op, silently, if not currently active — safe to call defensively (e.g. a
        /// pool releasing something that may already be inactive).</summary>
        public void Deactivate()
        {
            if (State != GameplayObjectLifecycleState.Active)
            {
                return;
            }

            _target.Deactivate();
            State = GameplayObjectLifecycleState.Deactivated;
        }

        /// <summary>Idempotent: a second call is a no-op. Deactivates first if still active, so
        /// Dispose always sees a symmetric Deactivate beforehand.</summary>
        public void Dispose()
        {
            if (State == GameplayObjectLifecycleState.Destroyed)
            {
                return;
            }

            if (State == GameplayObjectLifecycleState.Active)
            {
                Deactivate();
            }

            _target.Dispose();
            State = GameplayObjectLifecycleState.Destroyed;
        }
    }
}
