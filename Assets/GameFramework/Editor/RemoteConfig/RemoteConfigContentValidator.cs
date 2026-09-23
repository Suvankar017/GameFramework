using System.Collections.Generic;
using GameFramework.RemoteConfig;
using GameFramework.RemoteConfig.LiveOps;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.RemoteConfig
{
    /// <summary>
    /// Authoring-time checks for Phase 17 content assets - see CLAUDE.md's Phase 17 brief, section
    /// 18 ("editor validation should catch problems before runtime when practical"). Follows the same
    /// "scan every asset of a type in the project" pattern as
    /// <c>Monetization.MonetizationConfigValidator</c>. Warnings only (never hard errors) - content is
    /// frequently still being authored.
    /// </summary>
    internal static class RemoteConfigContentValidator
    {
        [MenuItem("GameFramework/Remote Config/Validate Configuration")]
        private static void ValidateAll()
        {
            int issues = 0;
            issues += ValidateRemoteConfigConfigurations();
            issues += ValidateLiveOpsConfigurations();

            Debug.Log(issues == 0
                ? "[RemoteConfig] Configuration validated with no issues."
                : $"[RemoteConfig] Configuration validation found {issues} issue(s). See warnings above.");
        }

        private static int ValidateRemoteConfigConfigurations()
        {
            int issues = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(RemoteConfigConfiguration)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<RemoteConfigConfiguration>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null)
                {
                    continue;
                }

                var seenKeys = new HashSet<string>();
                foreach (RemoteConfigDefinition definition in config.Definitions)
                {
                    if (!definition.IsKeyValid)
                    {
                        Debug.LogWarning($"[RemoteConfig] RemoteConfigConfiguration '{config.name}' has a definition with no Key assigned.", config);
                        issues++;
                        continue;
                    }

                    if (!seenKeys.Add(definition.Key))
                    {
                        Debug.LogWarning($"[RemoteConfig] RemoteConfigConfiguration '{config.name}' has duplicate key '{definition.Key}'.", config);
                        issues++;
                    }

                    if (definition.HasRange && definition.MinValue > definition.MaxValue)
                    {
                        Debug.LogWarning($"[RemoteConfig] Definition '{definition.Key}' in '{config.name}' has Min greater than Max.", config);
                        issues++;
                    }
                }
            }

            return issues;
        }

        private static int ValidateLiveOpsConfigurations()
        {
            int issues = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(LiveOpsConfiguration)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<LiveOpsConfiguration>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null)
                {
                    continue;
                }

                var seenIds = new HashSet<string>();
                foreach (LiveEventDefinition definition in config.Events)
                {
                    if (!definition.Id.IsValid)
                    {
                        Debug.LogWarning($"[RemoteConfig] LiveOpsConfiguration '{config.name}' has an event with no Id assigned.", config);
                        issues++;
                        continue;
                    }

                    if (!seenIds.Add(definition.Id.Value))
                    {
                        Debug.LogWarning($"[RemoteConfig] LiveOpsConfiguration '{config.name}' has duplicate event Id '{definition.Id}'.", config);
                        issues++;
                    }

                    bool hasStart = definition.TryGetStartUtc(out System.DateTime start);
                    bool hasEnd = definition.TryGetEndUtc(out System.DateTime end);

                    if (!hasStart || !hasEnd)
                    {
                        Debug.LogWarning($"[RemoteConfig] LiveEvent '{definition.Id}' in '{config.name}' has an unparsable start/end time - it will resolve as Invalid at runtime.", config);
                        issues++;
                    }
                    else if (end <= start)
                    {
                        Debug.LogWarning($"[RemoteConfig] LiveEvent '{definition.Id}' in '{config.name}' has an end time at or before its start time.", config);
                        issues++;
                    }
                }
            }

            return issues;
        }
    }
}
