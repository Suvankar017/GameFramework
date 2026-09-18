using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Presentation.Tests
{
    /// <summary>See <c>GameFramework.Tutorials.Tests.TestDefinitions</c>'s remarks - same
    /// reflection-onto-private-[SerializeField] pattern for <see cref="FeedbackDefinition.Id"/>/
    /// <see cref="FeedbackDefinition.Priority"/>; every per-channel config (<see cref="FeedbackDefinition.Audio"/>,
    /// ...) exposes a mutable object with public fields, so a test can set those directly on the
    /// instance this returns without any further reflection.</summary>
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static FeedbackDefinition Create(string id, FeedbackPriority priority = FeedbackPriority.Normal)
        {
            var definition = ScriptableObject.CreateInstance<FeedbackDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_priority", priority);
            return definition;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
