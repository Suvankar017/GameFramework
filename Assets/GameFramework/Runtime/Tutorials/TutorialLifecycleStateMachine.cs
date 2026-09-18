using System;
using System.Collections.Generic;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// Lightweight, reusable state machine for <see cref="TutorialState"/> - a fixed
    /// allowed-transition table plus a bounded, development-oriented history, in the same spirit as
    /// <see cref="GameFlow.LevelFlowStateMachine"/> (concrete, not a generic
    /// <c>StateMachine&lt;T&gt;</c> framework - Phase 9 ships exactly one state machine, so a
    /// generic abstraction would have no second user to justify it).
    ///
    /// Internal: only <see cref="TutorialService"/> drives transitions. Every transition is
    /// synchronous and single-threaded, so this only needs to guard the instant of mutation itself -
    /// the real re-entrancy protection for a whole command (which may publish events that call back
    /// into <see cref="TutorialService"/>) lives there, not here (see
    /// <see cref="TutorialService"/>'s remarks on re-entrancy).
    /// </summary>
    internal sealed class TutorialLifecycleStateMachine
    {
        private const int HistoryCapacity = 32;

        private static readonly Dictionary<TutorialState, TutorialState[]> AllowedTransitions =
            new Dictionary<TutorialState, TutorialState[]>
            {
                [TutorialState.Inactive] = new[] { TutorialState.Starting },
                [TutorialState.Starting] = new[] { TutorialState.Running },
                [TutorialState.Running] = new[] { TutorialState.Paused, TutorialState.Completing, TutorialState.Cancelling },
                [TutorialState.Paused] = new[] { TutorialState.Running, TutorialState.Completing, TutorialState.Cancelling },
                [TutorialState.Completing] = new[] { TutorialState.Completed },
                // Completed/Cancelled are terminal-but-transient: TutorialService immediately
                // transitions back to Inactive after publishing the outcome event, freeing the "one
                // active tutorial" slot without requiring a separate reset call. Completed can also
                // go straight to Starting - ITutorialService.Restart bypasses the Inactive step.
                [TutorialState.Completed] = new[] { TutorialState.Inactive, TutorialState.Starting },
                [TutorialState.Cancelling] = new[] { TutorialState.Cancelled },
                [TutorialState.Cancelled] = new[] { TutorialState.Inactive, TutorialState.Starting }
            };

        private readonly List<TutorialState> _history = new List<TutorialState>(HistoryCapacity) { TutorialState.Inactive };
        private bool _isTransitioning;

        public TutorialState Current { get; private set; } = TutorialState.Inactive;

        public event Action<TutorialState, TutorialState> StateChanged;

        /// <summary>Bounded (<see cref="HistoryCapacity"/>), oldest-first, development-oriented
        /// diagnostic only.</summary>
        public IReadOnlyList<TutorialState> History => _history;

        public bool TryTransition(TutorialState target)
        {
            if (_isTransitioning || target == Current)
            {
                return false;
            }

            if (!AllowedTransitions.TryGetValue(Current, out TutorialState[] allowed) || Array.IndexOf(allowed, target) < 0)
            {
                return false;
            }

            TutorialState previous = Current;

            _isTransitioning = true;
            try
            {
                Current = target;
                AppendHistory(target);
            }
            finally
            {
                _isTransitioning = false;
            }

            StateChanged?.Invoke(previous, Current);
            return true;
        }

        private void AppendHistory(TutorialState state)
        {
            if (_history.Count >= HistoryCapacity)
            {
                _history.RemoveAt(0);
            }

            _history.Add(state);
        }
    }
}
