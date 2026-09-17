using System.Reflection;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using UnityEngine;

namespace GameFramework.Rewards.Tests
{
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

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

        public static Unlocks.UnlockDefinition Unlock(string id)
        {
            var definition = ScriptableObject.CreateInstance<Unlocks.UnlockDefinition>();
            SetField(definition, "_id", id);
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
