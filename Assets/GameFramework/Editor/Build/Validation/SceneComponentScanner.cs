using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace GameFramework.Editor.Build
{
    /// <summary>One MonoBehaviour found in a scene/prefab file, with its serialized YAML block.</summary>
    public sealed class ScannedComponent
    {
        public ScannedComponent(string sourcePath, string rootScenePath, Type scriptType, string yaml)
        {
            SourcePath = sourcePath;
            RootScenePath = rootScenePath;
            ScriptType = scriptType;
            Yaml = yaml;
        }

        /// <summary>The scene or prefab file the component is serialized in.</summary>
        public string SourcePath { get; }

        /// <summary>The build scene through which the component was reached (equal to
        /// <see cref="SourcePath"/> for a component placed directly in a scene).</summary>
        public string RootScenePath { get; }

        /// <summary>The component's class, or null when its script is missing.</summary>
        public Type ScriptType { get; }
        public string Yaml { get; }

        public bool Is<T>() => ScriptType != null && typeof(T).IsAssignableFrom(ScriptType);

        /// <summary>The raw value of a top-level serialized field (e.g. <c>"1"</c> for a bool).</summary>
        public string GetField(string fieldName) => SceneComponentScanner.ReadField(Yaml, fieldName);

        /// <summary>The asset GUID of an object-reference field, or null if unset/scene-local.</summary>
        public string GetReferenceGuid(string fieldName) => SceneComponentScanner.ReadReferenceGuid(Yaml, fieldName);
    }

    /// <summary>
    /// Reads the build scenes (and the prefabs they instantiate) as <b>text YAML</b>, so validation can
    /// tell which framework bootstrappers a build contains and how they are configured. Scenes are
    /// never opened in the Editor, so validation cannot dirty or modify them.
    ///
    /// Deliberately shallow; it is not a dependency analyzer:
    /// <list type="bullet">
    /// <item>It resolves each MonoBehaviour's script GUID to its class, so a game's own bootstrapper
    /// subclass is recognized as, e.g., a <c>MonetizationBootstrapper</c>.</item>
    /// <item>It follows prefab instances recursively. A field override on a prefab instance is reported
    /// through <see cref="PrefabOverrides"/>, not merged into the component.</item>
    /// <item>It requires Force Text serialization (Unity's default). A binary scene is reported as
    /// unscannable rather than guessed at.</item>
    /// </list>
    /// </summary>
    public sealed class SceneComponentScanner
    {
        private static readonly Regex ScriptGuidPattern = new Regex(@"m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})");
        private static readonly Regex SourcePrefabPattern = new Regex(@"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})");
        private static readonly Regex OverridePattern = new Regex(@"propertyPath: (\S+)\s*\r?\n\s*value: ([^\r\n]*)");

        private readonly Dictionary<string, Type> _typeByGuid = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly HashSet<string> _visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<ScannedComponent> Components { get; } = new List<ScannedComponent>();

        /// <summary>Prefab-instance property overrides found in scanned scenes: (source, propertyPath, value).</summary>
        public List<(string Source, string PropertyPath, string Value)> PrefabOverrides { get; } = new List<(string, string, string)>();

        /// <summary>Script GUIDs referenced by a component whose script no longer exists.</summary>
        public List<(string Source, string Guid)> MissingScripts { get; } = new List<(string, string)>();

        public List<string> UnscannableFiles { get; } = new List<string>();

        public void ScanFile(string assetPath) => ScanFile(assetPath, assetPath);

        private void ScanFile(string assetPath, string rootScenePath)
        {
            if (string.IsNullOrEmpty(assetPath) || !_visited.Add(assetPath) || !File.Exists(assetPath))
            {
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(assetPath);
            }
            catch (IOException)
            {
                UnscannableFiles.Add(assetPath);
                return;
            }

            if (!text.StartsWith("%YAML", StringComparison.Ordinal))
            {
                UnscannableFiles.Add(assetPath);
                return;
            }

            ScanText(assetPath, rootScenePath, text);
        }

        /// <summary>Scans YAML text directly (also the unit-test entry point).</summary>
        public void ScanText(string sourcePath, string yaml) => ScanText(sourcePath, sourcePath, yaml);

        private void ScanText(string sourcePath, string rootScenePath, string yaml)
        {
            foreach (string document in yaml.Split(new[] { "\n--- " }, StringSplitOptions.None))
            {
                if (document.StartsWith("!u!114 ", StringComparison.Ordinal) || document.Contains("\nMonoBehaviour:"))
                {
                    Match script = ScriptGuidPattern.Match(document);
                    if (script.Success)
                    {
                        string guid = script.Groups[1].Value;
                        Type type = ResolveScriptType(guid);

                        // Only a GUID that resolves to no asset at all is a missing script. A script
                        // compiled into a DLL has a real asset but may not expose a MonoScript class.
                        if (type == null && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                        {
                            MissingScripts.Add((sourcePath, guid));
                        }

                        Components.Add(new ScannedComponent(sourcePath, rootScenePath, type, document));
                    }
                }
                else if (document.StartsWith("!u!1001 ", StringComparison.Ordinal))
                {
                    foreach (Match match in OverridePattern.Matches(document))
                    {
                        PrefabOverrides.Add((sourcePath, match.Groups[1].Value, match.Groups[2].Value.Trim()));
                    }

                    Match prefab = SourcePrefabPattern.Match(document);
                    if (prefab.Success)
                    {
                        ScanFile(AssetDatabase.GUIDToAssetPath(prefab.Groups[1].Value), rootScenePath);
                    }
                }
            }
        }

        public IEnumerable<ScannedComponent> OfType<T>()
        {
            foreach (ScannedComponent component in Components)
            {
                if (component.Is<T>())
                {
                    yield return component;
                }
            }
        }

        internal static string ReadField(string yaml, string fieldName)
        {
            Match match = Regex.Match(yaml, @"(?m)^  " + Regex.Escape(fieldName) + @":[ ]?(.*)$");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        internal static string ReadReferenceGuid(string yaml, string fieldName)
        {
            string value = ReadField(yaml, fieldName);
            if (value == null)
            {
                return null;
            }

            Match match = Regex.Match(value, @"guid: ([0-9a-f]{32})");
            return match.Success ? match.Groups[1].Value : null;
        }

        private Type ResolveScriptType(string guid)
        {
            if (_typeByGuid.TryGetValue(guid, out Type cached))
            {
                return cached;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            Type type = script != null ? script.GetClass() : null;
            _typeByGuid[guid] = type;
            return type;
        }
    }
}
