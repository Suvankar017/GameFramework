using System;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Events;

namespace GameFramework.Tutorials.Steps
{
    /// <summary>
    /// Completes when a matching <typeparamref name="TEvent"/> is published through
    /// <see cref="IEventService"/> - a framework event or a game-defined one (e.g.
    /// <c>CarStartedMovingEvent</c>); this type never knows what the payload means, only that one
    /// arrived and, optionally, that <paramref name="predicate"/> accepted it. Subscribes in
    /// <see cref="TutorialStepBase.OnBegin"/> and unsubscribes in
    /// <see cref="TutorialStepBase.OnEnd"/>, so a step that is cancelled (tutorial skipped/cancelled
    /// mid-step) never leaves a dangling subscription - see CLAUDE.md's Phase 9 brief section 32.
    /// </summary>
    public sealed class EventStep<TEvent> : TutorialStepBase
    {
        private readonly IEventService _events;
        private readonly Func<TEvent, bool> _predicate;

        public EventStep(string id, IEventService events, Func<TEvent, bool> predicate = null) : base(id)
        {
            _events = Guard.NotNull(events, nameof(events));
            _predicate = predicate;
        }

        protected override void OnBegin()
        {
            _events.Subscribe<TEvent>(OnEventPublished);
        }

        protected override void OnEnd()
        {
            _events.Unsubscribe<TEvent>(OnEventPublished);
        }

        private void OnEventPublished(TEvent payload)
        {
            if (_predicate == null || _predicate(payload))
            {
                Complete();
            }
        }
    }
}
