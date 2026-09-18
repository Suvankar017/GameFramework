using GameFramework.Core.Validation;
using GameFramework.Tutorials.Conditions;

namespace GameFramework.Tutorials.Steps
{
    /// <summary>
    /// Completes once a wrapped <see cref="ITutorialCondition"/> is satisfied. Checked once
    /// immediately when the step begins (covers the case where it is already true) and then once
    /// per tick while active - cheap and deterministic at this framework's expected scale (at most
    /// one active step, for at most one active tutorial, per CLAUDE.md's Phase 9 brief section 43),
    /// unlike <c>GameFramework.Quests</c>' statistic-indexed event-driven evaluation, which exists
    /// specifically to avoid polling a potentially large set of simultaneously-active quests/
    /// objectives - a concern this type does not have.
    /// </summary>
    public sealed class ConditionStep : TutorialStepBase
    {
        private readonly ITutorialCondition _condition;

        public ConditionStep(string id, ITutorialCondition condition) : base(id)
        {
            _condition = Guard.NotNull(condition, nameof(condition));
        }

        public ITutorialCondition Condition => _condition;

        protected override void OnBegin()
        {
            if (_condition.IsSatisfied())
            {
                Complete();
            }
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (State == TutorialStepState.Active && _condition.IsSatisfied())
            {
                Complete();
            }
        }
    }
}
