using System.Collections.Generic;
using GameFramework.Progression.Statistics;
using GameFramework.Quests.Achievements;
using GameFramework.Quests.Milestones;
using GameFramework.Quests.Quests;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Quests
{
    /// <summary>
    /// Lightweight authoring-time checks for Phase 7 content assets, following the same pattern as
    /// <c>GameplayConfigValidator</c>: validate the currently selected asset, or scan the whole
    /// project for duplicate ids of one type.
    /// </summary>
    internal static class QuestContentValidator
    {
        [MenuItem("GameFramework/Quests/Validate Selected Content")]
        private static void ValidateSelected()
        {
            Object selected = Selection.activeObject;

            switch (selected)
            {
                case StatisticDefinition statistic:
                    ValidateStatistic(statistic);
                    break;
                case QuestDefinition quest:
                    ValidateQuest(quest);
                    break;
                case AchievementDefinition achievement:
                    ValidateAchievement(achievement);
                    break;
                case MilestoneDefinition milestone:
                    ValidateMilestone(milestone);
                    break;
                default:
                    Debug.LogWarning("[Quests] Select a StatisticDefinition, QuestDefinition, " +
                        "AchievementDefinition, or MilestoneDefinition asset in the Project window first.");
                    break;
            }
        }

        [MenuItem("GameFramework/Quests/Find Duplicate Content Ids")]
        private static void FindDuplicateIds()
        {
            int duplicateCount = 0;
            duplicateCount += FindDuplicates<StatisticDefinition>(d => d.Id.Value);
            duplicateCount += FindDuplicates<QuestDefinition>(d => d.Id.Value);
            duplicateCount += FindDuplicates<AchievementDefinition>(d => d.Id.Value);
            duplicateCount += FindDuplicates<MilestoneDefinition>(d => d.Id.Value);

            Debug.Log(duplicateCount == 0
                ? "[Quests] No duplicate content Ids found."
                : $"[Quests] Found {duplicateCount} duplicate content Id(s). See warnings above.");
        }

        private static int FindDuplicates<T>(System.Func<T, string> getId) where T : Object
        {
            var seen = new Dictionary<string, T>();
            int duplicateCount = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                string id = definition == null ? null : getId(definition);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (seen.TryGetValue(id, out T existing))
                {
                    Debug.LogWarning($"[Quests] Duplicate {typeof(T).Name} Id '{id}' on '{definition.name}' " +
                        $"and '{existing.name}'.", definition);
                    duplicateCount++;
                }
                else
                {
                    seen.Add(id, definition);
                }
            }

            return duplicateCount;
        }

        private static void ValidateStatistic(StatisticDefinition definition)
        {
            int issues = 0;

            if (!definition.Id.IsValid)
            {
                Debug.LogWarning($"[Quests] StatisticDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            if (definition.MaxValue > 0 && definition.MinValue > definition.MaxValue)
            {
                Debug.LogWarning($"[Quests] StatisticDefinition '{definition.name}' has MinValue greater than MaxValue.", definition);
                issues++;
            }

            LogResult(definition.name, issues);
        }

        private static void ValidateQuest(QuestDefinition definition)
        {
            int issues = 0;

            if (!definition.Id.IsValid)
            {
                Debug.LogWarning($"[Quests] QuestDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            if (definition.CompletionRule == QuestCompletionRule.Count && definition.RequiredObjectiveCount < 1)
            {
                Debug.LogWarning($"[Quests] QuestDefinition '{definition.name}' uses Count completion but RequiredObjectiveCount < 1.", definition);
                issues++;
            }

            if (definition.RepeatPolicy == QuestRepeatPolicy.LimitedRepeats && definition.MaxRepeats < 1)
            {
                Debug.LogWarning($"[Quests] QuestDefinition '{definition.name}' uses LimitedRepeats but MaxRepeats < 1.", definition);
                issues++;
            }

            LogResult(definition.name, issues);
        }

        private static void ValidateAchievement(AchievementDefinition definition)
        {
            int issues = 0;

            if (!definition.Id.IsValid)
            {
                Debug.LogWarning($"[Quests] AchievementDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            LogResult(definition.name, issues);
        }

        private static void ValidateMilestone(MilestoneDefinition definition)
        {
            int issues = 0;

            if (!definition.Id.IsValid)
            {
                Debug.LogWarning($"[Quests] MilestoneDefinition '{definition.name}' has no Id assigned.", definition);
                issues++;
            }

            if (!definition.StatisticId.IsValid)
            {
                Debug.LogWarning($"[Quests] MilestoneDefinition '{definition.name}' has no Statistic Id assigned.", definition);
                issues++;
            }

            if (definition.Threshold <= 0)
            {
                Debug.LogWarning($"[Quests] MilestoneDefinition '{definition.name}' has a non-positive Threshold.", definition);
                issues++;
            }

            LogResult(definition.name, issues);
        }

        private static void LogResult(string assetName, int issueCount)
        {
            Debug.Log(issueCount == 0
                ? $"[Quests] '{assetName}' validated with no issues."
                : $"[Quests] '{assetName}' validation found {issueCount} issue(s). See warnings above.");
        }
    }
}
