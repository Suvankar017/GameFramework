using GameFramework.Gameplay.Objectives;
using GameFramework.Quests.Conditions;

namespace GameFramework.Quests.Quests
{
    /// <summary>
    /// One objective slot in a quest, supplied to <see cref="IQuestService.RegisterQuest"/> at
    /// composition-root time. Reuses Phase 4's <see cref="ObjectiveDefinition"/> for id/display text
    /// rather than introducing a second "objective text" asset type, and pairs it with the
    /// <see cref="ICondition"/> that must be satisfied — composed in code for the same reason
    /// <see cref="GameFramework.Rewards.RewardDefinition"/>'s content is (see its remarks): avoids a
    /// <c>[SerializeReference]</c> custom-drawer just to author condition trees in the Inspector.
    /// </summary>
    public readonly struct QuestObjectiveEntry
    {
        public readonly ObjectiveDefinition Definition;
        public readonly ICondition Condition;

        public QuestObjectiveEntry(ObjectiveDefinition definition, ICondition condition)
        {
            Definition = definition;
            Condition = condition;
        }
    }
}
