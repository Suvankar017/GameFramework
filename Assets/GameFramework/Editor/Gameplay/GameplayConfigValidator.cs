using System.Collections.Generic;
using GameFramework.Gameplay.Objectives;
using GameFramework.Gameplay.Pooling;
using GameFramework.Gameplay.Spawning;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Gameplay
{
    /// <summary>
    /// Lightweight authoring-time checks for Phase 4 configuration assets: missing prefabs on the
    /// selected <see cref="PoolConfiguration"/>/<see cref="SpawnConfiguration"/>, and duplicate
    /// <see cref="ObjectiveDefinition.Id"/> values across every such asset in the project — a
    /// duplicate would otherwise only surface as confusing behavior at runtime.
    /// </summary>
    internal static class GameplayConfigValidator
    {
        [MenuItem("GameFramework/Gameplay/Validate Selected Configuration")]
        private static void ValidateSelected()
        {
            Object selected = Selection.activeObject;

            switch (selected)
            {
                case PoolConfiguration poolConfig:
                    ValidatePoolConfiguration(poolConfig);
                    break;
                case SpawnConfiguration spawnConfig:
                    ValidateSpawnConfiguration(spawnConfig);
                    break;
                case ObjectiveDefinition objectiveDefinition:
                    ValidateObjectiveDefinition(objectiveDefinition);
                    break;
                default:
                    Debug.LogWarning("[Gameplay] Select a PoolConfiguration, SpawnConfiguration, or " +
                        "ObjectiveDefinition asset in the Project window first.");
                    break;
            }
        }

        [MenuItem("GameFramework/Gameplay/Find Duplicate Objective Ids")]
        private static void FindDuplicateObjectiveIds()
        {
            var seen = new Dictionary<string, ObjectiveDefinition>();
            int duplicateCount = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(ObjectiveDefinition)}"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<ObjectiveDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                if (seen.TryGetValue(definition.Id, out ObjectiveDefinition existing))
                {
                    Debug.LogWarning($"[Gameplay] Duplicate Objective Id '{definition.Id}' on '{definition.name}' " +
                        $"and '{existing.name}'.", definition);
                    duplicateCount++;
                }
                else
                {
                    seen.Add(definition.Id, definition);
                }
            }

            Debug.Log(duplicateCount == 0
                ? "[Gameplay] No duplicate Objective Ids found."
                : $"[Gameplay] Found {duplicateCount} duplicate Objective Id(s). See warnings above.");
        }

        private static void ValidatePoolConfiguration(PoolConfiguration config)
        {
            int issues = 0;

            if (config.Prefab == null)
            {
                Debug.LogWarning($"[Gameplay] PoolConfiguration '{config.name}' has no Prefab assigned.", config);
                issues++;
            }

            if (config.MaxSize < 1)
            {
                Debug.LogWarning($"[Gameplay] PoolConfiguration '{config.name}' has MaxSize < 1.", config);
                issues++;
            }

            LogResult(config.name, issues);
        }

        private static void ValidateSpawnConfiguration(SpawnConfiguration config)
        {
            int issues = 0;

            if (config.Prefab == null)
            {
                Debug.LogWarning($"[Gameplay] SpawnConfiguration '{config.name}' has no Prefab assigned.", config);
                issues++;
            }

            LogResult(config.name, issues);
        }

        private static void ValidateObjectiveDefinition(ObjectiveDefinition definition)
        {
            int issues = 0;

            if (string.IsNullOrEmpty(definition.Id))
            {
                Debug.LogWarning($"[Gameplay] ObjectiveDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            LogResult(definition.name, issues);
        }

        private static void LogResult(string assetName, int issueCount)
        {
            Debug.Log(issueCount == 0
                ? $"[Gameplay] '{assetName}' validated with no issues."
                : $"[Gameplay] '{assetName}' validation found {issueCount} issue(s). See warnings above.");
        }
    }
}
