using GameFramework.Core.Validation;
using GameFramework.Gameplay.Objectives;
using GameFramework.Quests.Conditions;
using GameFramework.Runtime.Events;
using UnityEngine;

namespace GameFramework.Quests.Objectives
{
    /// <summary>
    /// The framework's one concrete <see cref="ObjectiveBase"/> for Phase 7 content: an objective
    /// that completes itself once a wrapped <see cref="ICondition"/> is satisfied. This is the bridge
    /// between the Phase 4 objective state machine (which knows nothing about conditions or
    /// statistics) and Phase 7's condition system, deliberately extending
    /// <see cref="ObjectiveBase"/> rather than duplicating a second state machine.
    ///
    /// Never polled every frame - the owning <see cref="Quests.QuestService"/>/
    /// <see cref="Achievements.AchievementService"/>/<see cref="Milestones.MilestoneService"/> calls
    /// <see cref="Evaluate"/> only in response to a relevant gameplay event (see
    /// <see cref="ConditionStatisticIndex"/>), never from an <c>Update</c> loop.
    /// </summary>
    public sealed class ConditionObjective : ObjectiveBase, IProgressObjective
    {
        private readonly ICondition _condition;

        public ConditionObjective(string id, ICondition condition, IEventService events = null)
            : base(id, events)
        {
            _condition = Guard.NotNull(condition, nameof(condition));
        }

        public ICondition Condition => _condition;

        public int CurrentValue => _condition is IProgressCondition progress ? progress.CurrentValue : (IsConditionSatisfied ? 1 : 0);

        public int RequiredValue => _condition is IProgressCondition progress ? progress.RequiredValue : 1;

        public float ProgressNormalized => RequiredValue <= 0
            ? (IsConditionSatisfied ? 1f : 0f)
            : Mathf.Clamp01((float)CurrentValue / RequiredValue);

        private bool IsConditionSatisfied => _condition.IsSatisfied();

        /// <summary>Re-checks the wrapped condition and completes this objective if it is now
        /// satisfied. A no-op unless this objective is currently <see cref="ObjectiveState.Active"/>.</summary>
        public void Evaluate()
        {
            if (State != ObjectiveState.Active)
            {
                return;
            }

            if (_condition.IsSatisfied())
            {
                Complete();
            }
        }
    }
}
