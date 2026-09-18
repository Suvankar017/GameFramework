using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.GameFlow.Tests
{
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static LevelDefinition Level(string id, string sceneName, string mode = null)
        {
            var definition = ScriptableObject.CreateInstance<LevelDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_displayName", id);
            SetField(definition, "_sceneName", sceneName);
            SetField(definition, "_mode", mode);
            return definition;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
