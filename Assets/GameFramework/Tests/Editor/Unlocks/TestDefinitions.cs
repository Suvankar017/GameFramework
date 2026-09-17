using System.Reflection;
using UnityEngine;

namespace GameFramework.Unlocks.Tests
{
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static UnlockDefinition Unlock(string id)
        {
            var definition = ScriptableObject.CreateInstance<UnlockDefinition>();
            typeof(UnlockDefinition).GetField("_id", Flags).SetValue(definition, id);
            return definition;
        }
    }
}
