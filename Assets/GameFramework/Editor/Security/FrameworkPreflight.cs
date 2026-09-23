using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Purchases;
using GameFramework.RemoteConfig;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Security
{
    public enum PreflightSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>One preflight finding. <see cref="Source"/> is an asset/file path; never contains the
    /// offending value itself (a detected secret is reported by location and kind only).</summary>
    public readonly struct PreflightIssue
    {
        public readonly PreflightSeverity Severity;
        public readonly string Category;
        public readonly string Source;
        public readonly string Message;
        public readonly string SuggestedFix;

        public PreflightIssue(PreflightSeverity severity, string category, string source, string message, string suggestedFix)
        {
            Severity = severity;
            Category = category;
            Source = source ?? string.Empty;
            Message = message;
            SuggestedFix = suggestedFix ?? string.Empty;
        }
    }

    /// <summary>
    /// Phase 19 production preflight: a lightweight, read-only, explicitly-invoked scan for
    /// ship-blocking configuration mistakes - development-only mock providers left enabled in build
    /// scenes/prefabs, credential-looking strings committed under <c>Assets/</c> or
    /// <c>ProjectSettings/</c>, duplicate framework ids, and the current build profile.
    ///
    /// Complements (does not replace) the per-system validators under <c>Editor/*/</c>. Never modifies
    /// any asset or scene - it reads YAML/text only, so it does not open (or dirty) scenes. The pattern
    /// helpers are public and pure so they are unit-testable without touching the project.
    /// </summary>
    public static class FrameworkPreflight
    {
        // Serialized mock-toggle field names on the framework's bootstrappers.
        private static readonly string[] MockToggleFields =
        {
            "_useMockProviders", "_useMockProvider", "_useMockAnalyticsProvider", "_useMockCrashReportingProvider"
        };

        private static readonly (string Name, Regex Pattern)[] SecretPatterns =
        {
            ("Private key block", new Regex("-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----")),
            ("Google API key", new Regex("AIza[0-9A-Za-z_\\-]{35}")),
            ("AWS access key id", new Regex("\\bAKIA[0-9A-Z]{16}\\b")),
            ("Stripe live secret key", new Regex("\\bsk_live_[0-9A-Za-z]{16,}")),
            ("GitHub token", new Regex("\\bgh[pousr]_[0-9A-Za-z]{36,}")),
            ("Slack token", new Regex("\\bxox[abprs]-[0-9A-Za-z\\-]{10,}")),
            ("Hard-coded credential assignment", new Regex(
                "(?i)\\b(?:client_secret|api_secret|secret_key|private_key|password|access_token|refresh_token)\\s*[:=]\\s*\"[^\"\\s]{8,}\"")),
        };

        private static readonly string[] ScannedExtensions =
        {
            ".cs", ".asset", ".json", ".txt", ".xml", ".plist", ".prefab", ".unity", ".yaml", ".yml", ".properties", ".gradle"
        };

        /// <summary>Largest file the secret scan reads - bigger files are binary-ish assets and are skipped.</summary>
        private const long MaxScannedFileBytes = 2 * 1024 * 1024;

        public static List<PreflightIssue> RunAll()
        {
            var issues = new List<PreflightIssue>();
            CheckBuildProfile(issues);
            CheckMockToggles(issues);
            CheckDuplicateIds(issues);
            CheckSecrets(issues);
            return issues;
        }

        /// <summary>Only the committed-secret scan (Assets/ and ProjectSettings/). Reused by the Phase 20
        /// build pipeline's release-safety validation so the patterns live in one place.</summary>
        public static List<PreflightIssue> ScanForSecrets()
        {
            var issues = new List<PreflightIssue>();
            CheckSecrets(issues);
            return issues;
        }

        /// <summary>Returns the names of every secret pattern matched in <paramref name="text"/>, with
        /// the 1-based line of the first match. Never returns the matched text.</summary>
        public static List<(string PatternName, int Line)> FindSecretPatterns(string text)
        {
            var found = new List<(string, int)>();
            if (string.IsNullOrEmpty(text))
            {
                return found;
            }

            for (int i = 0; i < SecretPatterns.Length; i++)
            {
                Match match = SecretPatterns[i].Pattern.Match(text);
                if (match.Success)
                {
                    found.Add((SecretPatterns[i].Name, LineOf(text, match.Index)));
                }
            }

            return found;
        }

        /// <summary>Names of mock toggles serialized as enabled (<c>field: 1</c>) in Unity YAML text.</summary>
        public static List<string> FindEnabledMockToggles(string yaml)
        {
            var enabled = new List<string>();
            if (string.IsNullOrEmpty(yaml))
            {
                return enabled;
            }

            for (int i = 0; i < MockToggleFields.Length; i++)
            {
                if (Regex.IsMatch(yaml, "(?m)^\\s*" + MockToggleFields[i] + ":\\s*1\\s*$"))
                {
                    enabled.Add(MockToggleFields[i]);
                }
            }

            return enabled;
        }

        /// <summary>Every value that appears more than once (ordinal), each reported once. Null/empty
        /// values are ignored - a missing id is a different problem the per-system validators report.</summary>
        public static List<string> FindDuplicates(IEnumerable<string> values)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            var duplicates = new List<string>();

            foreach (string value in values)
            {
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (!seen.Add(value) && reported.Add(value))
                {
                    duplicates.Add(value);
                }
            }

            return duplicates;
        }

        private static void CheckBuildProfile(List<PreflightIssue> issues)
        {
            if (EditorUserBuildSettings.development)
            {
                issues.Add(new PreflightIssue(PreflightSeverity.Warning, "Build", "Build Settings",
                    "'Development Build' is enabled: DEVELOPMENT_BUILD is defined, so mock providers and simulation APIs are honored in the player.",
                    "Disable Development Build for a store/release build."));
            }
            else
            {
                issues.Add(new PreflightIssue(PreflightSeverity.Info, "Build", "Build Settings",
                    "Release build profile: mock providers are refused at runtime (DevelopmentProviderGuard).", null));
            }
        }

        private static void CheckMockToggles(List<PreflightIssue> issues)
        {
            var paths = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    paths.Add(scene.path);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (string path in paths)
            {
                string text = TryReadText(path);
                foreach (string toggle in FindEnabledMockToggles(text))
                {
                    issues.Add(new PreflightIssue(PreflightSeverity.Warning, "Providers", path,
                        $"Mock provider toggle '{toggle}' is enabled. It is ignored in release builds (NoOp providers are used and an error is logged), but it signals a real provider adapter is not wired up yet.",
                        "Disable the toggle and register a real provider adapter before shipping."));
                }
            }
        }

        private static void CheckDuplicateIds(List<PreflightIssue> issues)
        {
            foreach (ProductCatalog catalog in LoadAll<ProductCatalog>())
            {
                var ids = new List<string>();
                foreach (ProductDefinition product in catalog.Products)
                {
                    ids.Add(product?.Id.Value);
                }

                ReportDuplicates(issues, "Monetization", catalog, "product id", FindDuplicates(ids));
            }

            foreach (AdConfiguration config in LoadAll<AdConfiguration>())
            {
                var ids = new List<string>();
                foreach (AdPlacementConfig placement in config.Placements)
                {
                    ids.Add(placement?.Id.Value);
                }

                ReportDuplicates(issues, "Monetization", config, "ad placement id", FindDuplicates(ids));
            }

            foreach (RemoteConfigConfiguration config in LoadAll<RemoteConfigConfiguration>())
            {
                var keys = new List<string>();
                foreach (RemoteConfigDefinition definition in config.Definitions)
                {
                    keys.Add(definition != null ? definition.Key : null);
                }

                ReportDuplicates(issues, "Remote Config", config, "config key", FindDuplicates(keys));
            }
        }

        private static void CheckSecrets(List<PreflightIssue> issues)
        {
            foreach (string root in new[] { "Assets", "ProjectSettings" })
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!IsScannable(file))
                    {
                        continue;
                    }

                    foreach ((string patternName, int line) in FindSecretPatterns(TryReadText(file)))
                    {
                        issues.Add(new PreflightIssue(PreflightSeverity.Error, "Secrets", $"{file.Replace('\\', '/')}:{line}",
                            $"Possible credential ({patternName}). The value is not shown.",
                            "Move real secrets server-side; ship only provider-issued public client ids. If this was ever committed, rotate it."));
                    }
                }
            }
        }

        private static bool IsScannable(string file)
        {
            string normalized = file.Replace('\\', '/');
            if (normalized.Contains("/Tests/"))
            {
                return false; // Test fixtures intentionally contain fake credential-shaped strings.
            }

            string extension = Path.GetExtension(file);
            if (Array.IndexOf(ScannedExtensions, extension.ToLowerInvariant()) < 0)
            {
                return false;
            }

            try
            {
                return new FileInfo(file).Length <= MaxScannedFileBytes;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static void ReportDuplicates(List<PreflightIssue> issues, string category, ScriptableObject asset, string kind, List<string> duplicates)
        {
            foreach (string duplicate in duplicates)
            {
                issues.Add(new PreflightIssue(PreflightSeverity.Error, category, AssetDatabase.GetAssetPath(asset),
                    $"Duplicate {kind} '{duplicate}'.", $"Give every {kind} a unique, stable value."));
            }
        }

        private static IEnumerable<T> LoadAll<T>() where T : ScriptableObject
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    yield return asset;
                }
            }
        }

        private static string TryReadText(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (IOException)
            {
                return null; // Locked/unreadable file: skipped; the scan is advisory, not exhaustive.
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static int LineOf(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                }
            }

            return line;
        }
    }
}
