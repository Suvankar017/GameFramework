using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>See <c>GameFramework.GameFlow.Tests.TestDefinitions</c>'s remarks - same
    /// reflection-onto-private-[SerializeField] pattern, duplicated per test assembly.</summary>
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static TutorialDefinition Tutorial(
            string id,
            string[] prerequisites = null,
            TutorialRepeatPolicy repeatPolicy = TutorialRepeatPolicy.Once,
            TutorialSkipPolicy skipPolicy = TutorialSkipPolicy.Skippable,
            TutorialPausePolicy pausePolicy = TutorialPausePolicy.DoesNotPauseGameplay,
            TutorialPersistencePolicy persistencePolicy = TutorialPersistencePolicy.CompletionOnly)
        {
            var definition = ScriptableObject.CreateInstance<TutorialDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_displayLocalizationKey", $"Tutorial.{id}.Display");
            SetField(definition, "_descriptionLocalizationKey", $"Tutorial.{id}.Description");
            SetField(definition, "_prerequisites", prerequisites ?? System.Array.Empty<string>());
            SetField(definition, "_repeatPolicy", repeatPolicy);
            SetField(definition, "_skipPolicy", skipPolicy);
            SetField(definition, "_pausePolicy", pausePolicy);
            SetField(definition, "_persistencePolicy", persistencePolicy);
            return definition;
        }

        public static TutorialStepDefinition Step(string id, string localizationKey = null)
        {
            var definition = ScriptableObject.CreateInstance<TutorialStepDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_localizationKey", localizationKey ?? $"Step.{id}");
            return definition;
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
