using System;
using System.Collections.Generic;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Lightweight, reusable state machine for <see cref="LevelFlowState"/> - a fixed
    /// allowed-transition table plus a bounded, development-oriented history, in the same spirit as
    /// <see cref="Gameplay.Objectives.ObjectiveBase"/>'s Inactive/Active/Completed/Failed machine
    /// (concrete, not a generic <c>StateMachine&lt;T&gt;</c> framework - Phase 8 ships exactly one
    /// state machine, so a generic abstraction would have no second user to justify it).
    ///
    /// Internal: only <see cref="GameFlowService"/> drives transitions. Every transition is
    /// synchronous and single-threaded (this framework is main-thread-only throughout), so the
    /// re-entrancy guard only needs to cover the instant of mutation itself - the real re-entrancy
    /// protection for a whole flow *command* (which may publish events that call back into
    /// <see cref="GameFlowService"/>) lives there, not here.
    /// </summary>
    internal sealed class LevelFlowStateMachine
    {
        private const int HistoryCapacity = 32;

        private static readonly Dictionary<LevelFlowState, LevelFlowState[]> AllowedTransitions =
            new Dictionary<LevelFlowState, LevelFlowState[]>
            {
                [LevelFlowState.Unloaded] = new[] { LevelFlowState.Loading },
                [LevelFlowState.Loading] = new[] { LevelFlowState.Initializing, LevelFlowState.Exiting },
                [LevelFlowState.Initializing] = new[] { LevelFlowState.Ready, LevelFlowState.Exiting },
                [LevelFlowState.Ready] = new[] { LevelFlowState.Playing, LevelFlowState.Exiting },
                [LevelFlowState.Playing] = new[]
                {
                    LevelFlowState.Paused, LevelFlowState.Completing, LevelFlowState.Failing,
                    LevelFlowState.Restarting, LevelFlowState.Exiting
                },
                [LevelFlowState.Paused] = new[] { LevelFlowState.Playing, LevelFlowState.Restarting, LevelFlowState.Exiting },
                [LevelFlowState.Completing] = new[] { LevelFlowState.Completed },
                [LevelFlowState.Completed] = new[] { LevelFlowState.Restarting, LevelFlowState.Exiting },
                [LevelFlowState.Failing] = new[] { LevelFlowState.Failed },
                // Failed -> Playing exists solely for GameFlowService.Respawn(); no other command
                // ever requests it.
                [LevelFlowState.Failed] = new[] { LevelFlowState.Restarting, LevelFlowState.Exiting, LevelFlowState.Playing },
                [LevelFlowState.Restarting] = new[] { LevelFlowState.Loading, LevelFlowState.Ready },
                [LevelFlowState.Exiting] = new[] { LevelFlowState.Unloaded }
            };

        private readonly List<LevelFlowState> _history = new List<LevelFlowState>(HistoryCapacity) { LevelFlowState.Unloaded };
        private bool _isTransitioning;

        public LevelFlowState Current { get; private set; } = LevelFlowState.Unloaded;

        /// <summary>Raised after a transition completes, as (previous, current).</summary>
        public event Action<LevelFlowState, LevelFlowState> StateChanged;

        /// <summary>Bounded (<see cref="HistoryCapacity"/>), oldest-first, development-oriented
        /// diagnostic only - never assume this contains the full lifetime history.</summary>
        public IReadOnlyList<LevelFlowState> History => _history;

        public bool CanTransitionTo(LevelFlowState target)
        {
            return !_isTransitioning
                && target != Current
                && AllowedTransitions.TryGetValue(Current, out LevelFlowState[] allowed)
                && Array.IndexOf(allowed, target) >= 0;
        }

        public TransitionResult TryTransition(LevelFlowState target)
        {
            if (_isTransitioning)
            {
                return TransitionResult.TransitionBlocked;
            }

            if (target == Current)
            {
                return TransitionResult.AlreadyInState;
            }

            if (!AllowedTransitions.TryGetValue(Current, out LevelFlowState[] allowed) || Array.IndexOf(allowed, target) < 0)
            {
                return TransitionResult.InvalidTransition;
            }

            LevelFlowState previous = Current;

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
            return TransitionResult.Success;
        }

        private void AppendHistory(LevelFlowState state)
        {
            if (_history.Count >= HistoryCapacity)
            {
                _history.RemoveAt(0);
            }

            _history.Add(state);
        }
    }
}
