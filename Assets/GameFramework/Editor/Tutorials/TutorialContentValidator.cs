using System.Collections.Generic;
using GameFramework.Tutorials;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Tutorials
{
    /// <summary>
    /// Lightweight authoring-time checks for Phase 9 content assets, following the same pattern as
    /// <c>GameplayConfigValidator</c>/<c>QuestContentValidator</c>: validate the currently selected
    /// asset, or scan the whole project for duplicate/dangling ids across every asset of one type.
    /// </summary>
    internal static class TutorialContentValidator
    {
        [MenuItem("GameFramework/Tutorials/Validate Selected Content")]
        private static void ValidateSelected()
        {
            Object selected = Selection.activeObject;

            switch (selected)
            {
                case TutorialDefinition tutorial:
                    ValidateTutorial(tutorial);
                    break;
                case TutorialStepDefinition step:
                    ValidateStep(step);
                    break;
                default:
                    Debug.LogWarning("[Tutorials] Select a TutorialDefinition or TutorialStepDefinition asset in the Project window first.");
                    break;
            }
        }

        [MenuItem("GameFramework/Tutorials/Find Duplicate/Dangling Content Ids")]
        private static void FindDuplicateOrDanglingIds()
        {
            Dictionary<string, TutorialDefinition> tutorialsById = new Dictionary<string, TutorialDefinition>();
            int duplicateCount = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TutorialDefinition)}"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<TutorialDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null || !definition.Id.IsValid)
                {
                    continue;
                }

                string id = definition.Id.Value;
                if (tutorialsById.TryGetValue(id, out TutorialDefinition existing))
                {
                    Debug.LogWarning($"[Tutorials] Duplicate TutorialDefinition Id '{id}' on '{definition.name}' and '{existing.name}'.", definition);
                    duplicateCount++;
                }
                else
                {
                    tutorialsById.Add(id, definition);
                }
            }

            int danglingCount = 0;
            foreach (TutorialDefinition definition in tutorialsById.Values)
            {
                IReadOnlyList<TutorialId> prerequisites = definition.Prerequisites;
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    TutorialId prerequisite = prerequisites[i];
                    if (prerequisite.IsValid && !tutorialsById.ContainsKey(prerequisite.Value))
                    {
                        Debug.LogWarning($"[Tutorials] TutorialDefinition '{definition.name}' requires prerequisite " +
                            $"'{prerequisite}', which no TutorialDefinition asset in the project declares as its Id.", definition);
                        danglingCount++;
                    }
                }
            }

            var stepsById = new Dictionary<string, TutorialStepDefinition>();
            int duplicateStepCount = 0;
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(TutorialStepDefinition)}"))
            {
                var step = AssetDatabase.LoadAssetAtPath<TutorialStepDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (step == null || string.IsNullOrEmpty(step.Id))
                {
                    continue;
                }

                if (stepsById.TryGetValue(step.Id, out TutorialStepDefinition existing))
                {
                    Debug.LogWarning($"[Tutorials] Duplicate TutorialStepDefinition Id '{step.Id}' on '{step.name}' and '{existing.name}'.", step);
                    duplicateStepCount++;
                }
                else
                {
                    stepsById.Add(step.Id, step);
                }
            }

            int totalIssues = duplicateCount + danglingCount + duplicateStepCount;
            Debug.Log(totalIssues == 0
                ? "[Tutorials] No duplicate or dangling content Ids found."
                : $"[Tutorials] Found {totalIssues} issue(s). See warnings above.");
        }

        private static void ValidateTutorial(TutorialDefinition definition)
        {
            int issues = 0;

            if (!definition.Id.IsValid)
            {
                Debug.LogWarning($"[Tutorials] TutorialDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            IReadOnlyList<TutorialId> prerequisites = definition.Prerequisites;
            for (int i = 0; i < prerequisites.Count; i++)
            {
                if (prerequisites[i].IsValid && prerequisites[i] == definition.Id)
                {
                    Debug.LogWarning($"[Tutorials] TutorialDefinition '{definition.name}' lists itself as its own prerequisite.", definition);
                    issues++;
                }
            }

            LogResult(definition.name, issues);
        }

        private static void ValidateStep(TutorialStepDefinition definition)
        {
            int issues = 0;

            if (string.IsNullOrEmpty(definition.Id))
            {
                Debug.LogWarning($"[Tutorials] TutorialStepDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            if (string.IsNullOrEmpty(definition.LocalizationKey))
            {
                Debug.LogWarning($"[Tutorials] TutorialStepDefinition '{definition.name}' has no Localization Key assigned.", definition);
                issues++;
            }

            LogResult(definition.name, issues);
        }

        private static void LogResult(string assetName, int issueCount)
        {
            Debug.Log(issueCount == 0
                ? $"[Tutorials] '{assetName}' validated with no issues."
                : $"[Tutorials] '{assetName}' validation found {issueCount} issue(s). See warnings above.");
        }
    }
}
