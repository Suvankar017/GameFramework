using System.Reflection;
using GameFramework.Progression.Economy;
using GameFramework.Progression.Experience;
using GameFramework.Progression.Inventory;
using UnityEngine;

namespace GameFramework.Progression.Tests
{
    /// <summary>
    /// Builds definition ScriptableObjects with specific field values for tests. Definitions expose
    /// only read-only properties over <c>[SerializeField]</c> backing fields at runtime (see
    /// <see cref="CurrencyDefinition"/>'s remarks on why - they are immutable authoring data), so
    /// tests populate them via reflection instead of a public setter, the same trade-off
    /// <c>ObjectiveDefinition</c> already accepts elsewhere in the framework.
    /// </summary>
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static CurrencyDefinition Currency(string id, int maxBalance = 0, int minBalance = 0)
        {
            var definition = ScriptableObject.CreateInstance<CurrencyDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_maxBalance", maxBalance);
            SetField(definition, "_minBalance", minBalance);
            return definition;
        }

        public static ItemDefinition Item(string id, int maxStack = 0)
        {
            var definition = ScriptableObject.CreateInstance<ItemDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_maxStack", maxStack);
            return definition;
        }

        public static ProgressionCurveDefinition LinearCurve(int baseExperience, int incrementPerLevel, int maxLevel = 0)
        {
            var definition = ScriptableObject.CreateInstance<ProgressionCurveDefinition>();
            SetField(definition, "_mode", ProgressionCurveMode.Linear);
            SetField(definition, "_baseExperience", baseExperience);
            SetField(definition, "_incrementPerLevel", incrementPerLevel);
            SetField(definition, "_maxLevel", maxLevel);
            return definition;
        }

        public static ProgressionCurveDefinition TableCurve(params int[] requiredExperiencePerLevel)
        {
            var definition = ScriptableObject.CreateInstance<ProgressionCurveDefinition>();
            SetField(definition, "_mode", ProgressionCurveMode.Table);
            SetField(definition, "_table", requiredExperiencePerLevel);
            return definition;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
