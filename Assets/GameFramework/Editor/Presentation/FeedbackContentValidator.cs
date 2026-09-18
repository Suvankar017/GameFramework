using System.Collections.Generic;
using GameFramework.Presentation;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Presentation
{
    /// <summary>
    /// Lightweight authoring-time checks for Phase 10 content assets, following the same pattern as
    /// <c>QuestContentValidator</c>/<c>TutorialContentValidator</c>: validate the selected asset, or
    /// scan the project for duplicate ids and missing references across every asset of the type.
    /// </summary>
    internal static class FeedbackContentValidator
    {
        [MenuItem("GameFramework/Presentation/Validate Selected Definition")]
        private static void ValidateSelected()
        {
            if (Selection.activeObject is FeedbackDefinition definition)
            {
                ValidateDefinition(definition);
            }
            else
            {
                Debug.LogWarning("[Presentation] Select a FeedbackDefinition asset in the Project window first.");
            }
        }

        [MenuItem("GameFramework/Presentation/Find Duplicate Feedback Ids")]
        private static void FindDuplicateIds()
        {
            var seen = new Dictionary<string, FeedbackDefinition>();
            int duplicateCount = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(FeedbackDefinition)}"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<FeedbackDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null || !definition.Id.IsValid)
                {
                    continue;
                }

                string id = definition.Id.Value;
                if (seen.TryGetValue(id, out FeedbackDefinition existing))
                {
                    Debug.LogWarning($"[Presentation] Duplicate FeedbackDefinition Id '{id}' on '{definition.name}' " +
                        $"and '{existing.name}'.", definition);
                    duplicateCount++;
                }
                else
                {
                    seen.Add(id, definition);
                }
            }

            Debug.Log(duplicateCount == 0
                ? "[Presentation] No duplicate Feedback Ids found."
                : $"[Presentation] Found {duplicateCount} duplicate Feedback Id(s). See warnings above.");
        }

        private static void ValidateDefinition(FeedbackDefinition definition)
        {
            int issues = 0;

            if (!definition.Id.IsValid)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            if (definition.Audio.Enabled && definition.Audio.Cue == null)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has Audio enabled but no Cue assigned.", definition);
                issues++;
            }

            if (definition.Visual.Enabled && definition.Visual.EffectPrefab == null)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has Visual enabled but no EffectPrefab assigned.", definition);
                issues++;
            }

            if (definition.Camera.Enabled && definition.Camera.Amplitude <= 0f)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has Camera enabled but a non-positive Amplitude.", definition);
                issues++;
            }

            if (definition.Screen.Enabled && definition.Screen.Color.a <= 0f)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has Screen enabled but a fully transparent Color.", definition);
                issues++;
            }

            if (definition.Time.Enabled && definition.Time.Duration <= 0f)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has Time enabled but a non-positive Duration.", definition);
                issues++;
            }

            if (definition.UI.Enabled && string.IsNullOrEmpty(definition.UI.Tag))
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has UI enabled but no Tag assigned.", definition);
                issues++;
            }

            if (!definition.Audio.Enabled && !definition.Haptic.Enabled && !definition.Camera.Enabled &&
                !definition.Visual.Enabled && !definition.Screen.Enabled && !definition.UI.Enabled && !definition.Time.Enabled)
            {
                Debug.LogWarning($"[Presentation] FeedbackDefinition '{definition.name}' has no channel enabled; playing it would do nothing.", definition);
                issues++;
            }

            Debug.Log(issues == 0
                ? $"[Presentation] '{definition.name}' validated with no issues."
                : $"[Presentation] '{definition.name}' validation found {issues} issue(s). See warnings above.");
        }
    }
}
