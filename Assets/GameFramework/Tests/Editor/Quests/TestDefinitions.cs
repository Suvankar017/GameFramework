using System.Reflection;
using GameFramework.Gameplay.Objectives;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Achievements;
using GameFramework.Quests.Milestones;
using GameFramework.Quests.Quests;
using GameFramework.Rewards;
using UnityEngine;

namespace GameFramework.Quests.Tests
{
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static StatisticDefinition Statistic(
            string id,
            StatisticValueType valueType = StatisticValueType.Integer,
            bool isMonotonic = false,
            int minValue = 0,
            int maxValue = 0,
            bool persistent = true)
        {
            var definition = ScriptableObject.CreateInstance<StatisticDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_valueType", valueType);
            SetField(definition, "_isMonotonic", isMonotonic);
            SetField(definition, "_minValue", minValue);
            SetField(definition, "_maxValue", maxValue);
            SetField(definition, "_persistent", persistent);
            return definition;
        }

        public static ObjectiveDefinition Objective(string id, string displayName = null)
        {
            var definition = ScriptableObject.CreateInstance<ObjectiveDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_displayName", displayName ?? id);
            return definition;
        }

        public static QuestDefinition Quest(
            string id,
            QuestCompletionRule completionRule = QuestCompletionRule.All,
            int requiredObjectiveCount = 1,
            QuestRepeatPolicy repeatPolicy = QuestRepeatPolicy.OneTime,
            int maxRepeats = 1,
            string rewardId = "")
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_completionRule", completionRule);
            SetField(definition, "_requiredObjectiveCount", requiredObjectiveCount);
            SetField(definition, "_repeatPolicy", repeatPolicy);
            SetField(definition, "_maxRepeats", maxRepeats);
            SetField(definition, "_rewardId", rewardId);
            return definition;
        }

        public static AchievementDefinition Achievement(string id, string rewardId = "", bool autoClaimReward = false)
        {
            var definition = ScriptableObject.CreateInstance<AchievementDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_rewardId", rewardId);
            SetField(definition, "_autoClaimReward", autoClaimReward);
            return definition;
        }

        public static MilestoneDefinition Milestone(string id, string statisticId, int threshold, string rewardId = "")
        {
            var definition = ScriptableObject.CreateInstance<MilestoneDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_statisticId", statisticId);
            SetField(definition, "_threshold", threshold);
            SetField(definition, "_rewardId", rewardId);
            return definition;
        }

        public static Unlocks.UnlockDefinition Unlock(string id)
        {
            var definition = ScriptableObject.CreateInstance<Unlocks.UnlockDefinition>();
            SetField(definition, "_id", id);
            return definition;
        }

        public static CurrencyDefinition Currency(string id, int maxBalance = 0)
        {
            var definition = ScriptableObject.CreateInstance<CurrencyDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_maxBalance", maxBalance);
            return definition;
        }

        public static ItemDefinition Item(string id, int maxStack = 0)
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_maxStack", maxStack);
            return definition;
        }

        public static ProgressionCurveDefinition LinearCurve(int baseExperience, int incrementPerLevel)
        {
            var definition = ScriptableObject.CreateInstance<ProgressionCurveDefinition>();
            SetField(definition, "_mode", ProgressionCurveMode.Linear);
            SetField(definition, "_baseExperience", baseExperience);
            SetField(definition, "_incrementPerLevel", incrementPerLevel);
            return definition;
        }

        public static RewardDefinition Reward(string id, RewardClaimPolicy policy)
        {
            var definition = ScriptableObject.CreateInstance<RewardDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_claimPolicy", policy);
            return definition;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
