using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// A thin UI Toolkit front end for <see cref="FrameworkBuildPipeline"/>: pick a profile, optionally
    /// override version/build number, then Validate (no side effects) or Build. It is not a
    /// replacement for Unity's Build Settings window. Scenes, platform modules, and Player Settings
    /// stay there, and CI uses <see cref="CommandLineBuild"/> instead.
    /// </summary>
    public sealed class BuildPipelineWindow : EditorWindow
    {
        private readonly List<BuildValidationIssue> _issues = new List<BuildValidationIssue>();

        private ObjectField _profileField;
        private Label _profileSummary;
        private TextField _versionField;
        private TextField _buildNumberField;
        private Toggle _overwriteToggle;
        private Label _status;
        private Label _lastBuild;
        private ListView _issueList;
        private string _lastOutputDirectory;

        [MenuItem("GameFramework/Build/Build Pipeline")]
        private static void Open() => GetWindow<BuildPipelineWindow>("Build Pipeline");

        private void CreateGUI()
        {
            VisualTreeAsset tree = LoadAsset<VisualTreeAsset>("BuildPipelineWindow t:VisualTreeAsset");
            StyleSheet style = LoadAsset<StyleSheet>("BuildPipelineWindow t:StyleSheet");
            if (tree == null)
            {
                rootVisualElement.Add(new Label("BuildPipelineWindow.uxml was not found next to BuildPipelineWindow.cs."));
                return;
            }

            tree.CloneTree(rootVisualElement);
            if (style != null)
            {
                rootVisualElement.styleSheets.Add(style);
            }

            _profileField = rootVisualElement.Q<ObjectField>("profile");
            _profileField.objectType = typeof(FrameworkBuildProfile);
            _profileField.RegisterValueChangedCallback(_ => RefreshSummary());
            _profileSummary = rootVisualElement.Q<Label>("profile-summary");
            _versionField = rootVisualElement.Q<TextField>("version");
            _buildNumberField = rootVisualElement.Q<TextField>("build-number");
            _overwriteToggle = rootVisualElement.Q<Toggle>("overwrite");
            _status = rootVisualElement.Q<Label>("status");
            _lastBuild = rootVisualElement.Q<Label>("last-build");

            _issueList = rootVisualElement.Q<ListView>("issues");
            _issueList.itemsSource = _issues;
            _issueList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _issueList.makeItem = MakeRow;
            _issueList.bindItem = BindRow;

            rootVisualElement.Q<Button>("validate").clicked += () => Execute(buildAfterValidation: false);
            rootVisualElement.Q<Button>("build").clicked += () => Execute(buildAfterValidation: true);
            rootVisualElement.Q<Button>("open-output").clicked += OpenOutput;

            List<FrameworkBuildProfile> profiles = FrameworkBuildProfile.FindAll();
            if (profiles.Count > 0)
            {
                _profileField.value = profiles[0];
            }

            RefreshSummary();
        }

        private void RefreshSummary()
        {
            var profile = _profileField.value as FrameworkBuildProfile;
            _profileSummary.text = profile == null
                ? "Select a FrameworkBuildProfile (Create > GameFramework > Build > Build Profile)."
                : $"{profile.ProfileId}: {profile.Target} + {profile.Environment}, {(profile.DevelopmentBuild ? "Development Build" : "Release")}, " +
                  $"build number {profile.BuildNumberScheme}, output {profile.OutputRoot}/";
        }

        private void Execute(bool buildAfterValidation)
        {
            var profile = _profileField.value as FrameworkBuildProfile;
            if (profile == null)
            {
                _status.text = "Select a build profile first.";
                return;
            }

            var request = new BuildRequest(profile)
            {
                VersionOverride = string.IsNullOrWhiteSpace(_versionField.value) ? null : _versionField.value.Trim(),
                AllowOverwrite = _overwriteToggle.value
            };

            if (!string.IsNullOrWhiteSpace(_buildNumberField.value))
            {
                if (!int.TryParse(_buildNumberField.value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int buildNumber))
                {
                    _status.text = "Build number override must be an integer.";
                    return;
                }

                request.BuildNumberOverride = buildNumber;
            }

            var pipeline = new FrameworkBuildPipeline();
            BuildRunResult result = buildAfterValidation ? pipeline.Run(request) : pipeline.Validate(request);
            Show(result);
        }

        private void Show(BuildRunResult result)
        {
            _issues.Clear();
            if (result.Validation != null)
            {
                _issues.AddRange(result.Validation.Issues);
            }

            if (result.PostBuildValidation != null)
            {
                _issues.AddRange(result.PostBuildValidation.Issues);
            }

            _issues.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            _issueList.Rebuild();

            BuildValidationReport validation = result.Validation;
            _status.text = validation == null
                ? $"{result.Status}: {result.FailureMessage}"
                : $"{result.Status} - {validation.ErrorCount} error(s), {validation.WarningCount} warning(s), {validation.InfoCount} info, {validation.PassCount} passed" +
                  (result.FailureMessage != null ? $" - {result.FailureMessage}" : string.Empty);

            if (result.Context != null)
            {
                _lastOutputDirectory = result.Context.OutputDirectory;
            }

            if (result.MetadataPath != null)
            {
                BuildMetadata metadata = JsonUtility.FromJson<BuildMetadata>(File.ReadAllText(result.MetadataPath));
                _lastBuild.text = $"Last build: {metadata.ArtifactFileName} - {metadata.ApplicationVersion} ({metadata.BuildNumber}), " +
                                  $"{metadata.Environment}, {metadata.GitBranch}@{Short(metadata.GitCommit)}, {metadata.BuildTimestampUtc}";
            }
        }

        private void OpenOutput()
        {
            if (string.IsNullOrEmpty(_lastOutputDirectory) || !Directory.Exists(_lastOutputDirectory))
            {
                _status.text = "No output folder yet - run a build first.";
                return;
            }

            EditorUtility.RevealInFinder(_lastOutputDirectory);
        }

        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("gf-issue-row");
            row.Add(new Label { name = "text" });
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            BuildValidationIssue issue = _issues[index];
            var label = row.Q<Label>("text");
            label.text = $"{BuildValidationReport.Prefix(issue.Severity)} {issue.CheckId}: {issue.Message}" +
                         (string.IsNullOrEmpty(issue.SuggestedFix) || issue.Severity < BuildValidationSeverity.Warning ? string.Empty : $"\n    fix: {issue.SuggestedFix}");

            label.RemoveFromClassList("gf-issue--error");
            label.RemoveFromClassList("gf-issue--warning");
            label.RemoveFromClassList("gf-issue--pass");
            switch (issue.Severity)
            {
                case BuildValidationSeverity.Error: label.AddToClassList("gf-issue--error"); break;
                case BuildValidationSeverity.Warning: label.AddToClassList("gf-issue--warning"); break;
                case BuildValidationSeverity.Pass: label.AddToClassList("gf-issue--pass"); break;
            }
        }

        private static T LoadAsset<T>(string filter) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static string Short(string commit) => commit != null && commit.Length > 12 ? commit.Substring(0, 12) : commit;
    }
}
