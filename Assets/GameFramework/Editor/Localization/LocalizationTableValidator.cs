using System.Collections.Generic;
using GameFramework.Localization;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Localization
{
    /// <summary>
    /// Lightweight authoring-time checks for localization data: duplicate keys within a table,
    /// empty values, and key parity against the default language (a key present in one language
    /// but missing from another silently falls back at runtime — this makes that visible before
    /// runtime instead).
    /// </summary>
    internal static class LocalizationTableValidator
    {
        [MenuItem("GameFramework/Localization/Validate Selected Config")]
        private static void ValidateSelectedConfig()
        {
            if (!(Selection.activeObject is LocalizationConfigAsset config))
            {
                Debug.LogWarning("[Localization] Select a LocalizationConfigAsset in the Project window first.");
                return;
            }

            Validate(config);
        }

        internal static int Validate(LocalizationConfigAsset config)
        {
            int issueCount = 0;
            var defaultKeys = new HashSet<string>();
            LocalizationTableAsset defaultTable = null;

            foreach (LocalizationTableAsset table in config.Tables)
            {
                if (table == null)
                {
                    Debug.LogWarning($"[Localization] '{config.name}' references a null table.", config);
                    issueCount++;
                    continue;
                }

                if (table.LanguageCode == config.DefaultLanguageCode)
                {
                    defaultTable = table;
                }

                issueCount += ValidateTable(table, defaultKeys, isDefault: table.LanguageCode == config.DefaultLanguageCode);
            }

            if (defaultTable == null)
            {
                Debug.LogWarning(
                    $"[Localization] '{config.name}' has no table matching its default language code '{config.DefaultLanguageCode}'.", config);
                issueCount++;
            }
            else
            {
                foreach (LocalizationTableAsset table in config.Tables)
                {
                    if (table == null || table == defaultTable)
                    {
                        continue;
                    }

                    issueCount += ValidateParity(table, defaultTable);
                }
            }

            Debug.Log(issueCount == 0
                ? $"[Localization] '{config.name}' validated with no issues."
                : $"[Localization] '{config.name}' validation found {issueCount} issue(s). See warnings above.");

            return issueCount;
        }

        private static int ValidateTable(LocalizationTableAsset table, HashSet<string> defaultLanguageKeyAccumulator, bool isDefault)
        {
            int issues = 0;
            var seen = new HashSet<string>();

            foreach (LocalizationEntry entry in table.Entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    Debug.LogWarning($"[Localization] Table '{table.name}' has an entry with an empty key.", table);
                    issues++;
                    continue;
                }

                if (!seen.Add(entry.Key))
                {
                    Debug.LogWarning($"[Localization] Table '{table.name}' has duplicate key '{entry.Key}'.", table);
                    issues++;
                }

                if (string.IsNullOrEmpty(entry.Value))
                {
                    Debug.LogWarning($"[Localization] Table '{table.name}' key '{entry.Key}' has an empty value.", table);
                    issues++;
                }

                if (isDefault)
                {
                    defaultLanguageKeyAccumulator.Add(entry.Key);
                }
            }

            return issues;
        }

        private static int ValidateParity(LocalizationTableAsset table, LocalizationTableAsset defaultTable)
        {
            int issues = 0;
            var thisTableKeys = new HashSet<string>();
            foreach (LocalizationEntry entry in table.Entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    thisTableKeys.Add(entry.Key);
                }
            }

            foreach (LocalizationEntry defaultEntry in defaultTable.Entries)
            {
                if (string.IsNullOrEmpty(defaultEntry.Key) || thisTableKeys.Contains(defaultEntry.Key))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"[Localization] Table '{table.name}' is missing key '{defaultEntry.Key}' present in the default language.", table);
                issues++;
            }

            return issues;
        }
    }
}
