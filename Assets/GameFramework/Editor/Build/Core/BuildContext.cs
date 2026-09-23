using System.Collections.Generic;
using System.IO;
using GameFramework.Runtime.Security;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// A <see cref="BuildRequest"/> resolved into concrete values: the version, build number,
    /// application id, scenes, defines, and output paths that will actually be used. Validators check
    /// <i>these</i> values rather than whatever happens to be in Player Settings, so validation and the
    /// build always agree. Resolution failures are added to the report; they never throw.
    /// </summary>
    public sealed class BuildContext
    {
        private BuildContext(BuildRequest request)
        {
            Request = request;
            Profile = request.Profile;
        }

        public BuildRequest Request { get; }
        public FrameworkBuildProfile Profile { get; }

        public BuildTarget Target { get; private set; }
        public BuildTargetGroup TargetGroup { get; private set; }
        public DeploymentEnvironment Environment { get; private set; }

        /// <summary>True for Staging/Production: the environments release rules apply to.</summary>
        public bool IsReleaseEnvironment => Environment == DeploymentEnvironment.Staging || Environment == DeploymentEnvironment.Production;
        public bool IsProduction => Environment == DeploymentEnvironment.Production;

        public bool VersionValid { get; private set; }
        public SemanticVersion Version { get; private set; }
        public string VersionText { get; private set; }
        public int BuildNumber { get; private set; }
        public string ApplicationIdentifier { get; private set; }
        public IReadOnlyList<string> Scenes { get; private set; }
        public IReadOnlyList<string> PipelineDefines { get; private set; }

        /// <summary>Defines already in Player Settings for the target group (read-only here).</summary>
        public IReadOnlyList<string> ProjectDefines { get; private set; }

        public string ProjectRoot { get; private set; }
        public string OutputDirectory { get; private set; }
        public string ArtifactBaseName { get; private set; }

        /// <summary>The path handed to Unity (<c>BuildPlayerOptions.locationPathName</c>).</summary>
        public string ArtifactPath { get; private set; }

        public GitInfo Git { get; set; } = GitInfo.Unavailable("Not captured.");

        /// <summary>Components found in the build scenes (and their prefabs), filled once by the
        /// pipeline before validation so every validator shares one scan.</summary>
        public SceneComponentScanner SceneScan { get; set; } = new SceneComponentScanner();

        public static BuildContext Resolve(BuildRequest request, BuildValidationReport report)
        {
            var context = new BuildContext(request);
            FrameworkBuildProfile profile = request.Profile;

            context.ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            context.Target = profile.Target;
            context.TargetGroup = BuildPipeline.GetBuildTargetGroup(profile.Target);
            context.Environment = profile.Environment;

            context.ResolveVersion(report);
            context.ResolveBuildNumber(report);

            context.ApplicationIdentifier = string.IsNullOrEmpty(profile.ApplicationIdentifierOverride)
                ? PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(context.TargetGroup))
                : profile.ApplicationIdentifierOverride.Trim();

            context.Scenes = ResolveScenes(profile);
            context.PipelineDefines = BuildDefines.ForBuild(profile.Environment, profile.ExtraDefines);
            context.ProjectDefines = BuildDefines.Split(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(context.TargetGroup)));

            string root = string.IsNullOrEmpty(request.OutputRootOverride) ? profile.OutputRoot : request.OutputRootOverride;
            if (string.IsNullOrWhiteSpace(root))
            {
                root = "Builds";
            }

            string absoluteRoot = Path.IsPathRooted(root) ? root : Path.Combine(context.ProjectRoot, root);
            context.OutputDirectory = Path.GetFullPath(ArtifactNaming.OutputDirectory(absoluteRoot, context.Target, context.Environment));

            string slug = string.IsNullOrEmpty(profile.ArtifactSlug) ? PlayerSettings.productName : profile.ArtifactSlug;
            context.ArtifactBaseName = context.VersionValid
                ? ArtifactNaming.BaseName(slug, context.Target, context.Environment, context.Version, context.BuildNumber)
                : string.Empty;
            context.ArtifactPath = context.ArtifactBaseName.Length > 0
                ? ArtifactNaming.LocationPath(context.OutputDirectory, context.ArtifactBaseName, context.Target, profile.AndroidAppBundle)
                : string.Empty;

            return context;
        }

        /// <summary><c>{output}/{base}{suffix}</c> - e.g. ".build.json" metadata beside the artifact.</summary>
        public string SidecarPath(string suffix) => Path.Combine(OutputDirectory, ArtifactBaseName + suffix);

        private void ResolveVersion(BuildValidationReport report)
        {
            string text = !string.IsNullOrEmpty(Request.VersionOverride)
                ? Request.VersionOverride
                : !string.IsNullOrEmpty(Profile.VersionOverride) ? Profile.VersionOverride : PlayerSettings.bundleVersion;

            VersionText = text ?? string.Empty;
            if (SemanticVersion.TryParse(text, out SemanticVersion version, out string error))
            {
                Version = version;
                VersionValid = true;
                report.Pass("Version.Format", $"Application version {version}.");
            }
            else
            {
                report.Error("Version.Format", error, "Use MAJOR[.MINOR[.PATCH]] digits only, e.g. 1.4.0.");
            }
        }

        private void ResolveBuildNumber(BuildValidationReport report)
        {
            // Non-mobile targets have no store build number; they use Android's versionCode as the
            // project's neutral integer so artifact names still carry one deterministic number.
            int playerSettingsNumber = PlayerSettings.Android.bundleVersionCode;
            if (Target == BuildTarget.iOS && !BuildNumbers.TryParse(PlayerSettings.iOS.buildNumber, out playerSettingsNumber))
            {
                playerSettingsNumber = 0;
            }

            if (!VersionValid && Profile.BuildNumberScheme == BuildNumberScheme.EncodedFromVersion && !Request.BuildNumberOverride.HasValue)
            {
                return; // Already reported by the version check.
            }

            int number = BuildNumbers.Resolve(Profile.BuildNumberScheme, Version, playerSettingsNumber, Request.BuildNumberOverride, out string error);
            if (error != null)
            {
                report.Error("Version.BuildNumber", error, "Fix the build number source (Player Settings, the profile's scheme, or -buildNumber).");
                return;
            }

            BuildNumber = number;
            string source = Request.BuildNumberOverride.HasValue ? "explicit override" : Profile.BuildNumberScheme.ToString();
            report.Pass("Version.BuildNumber", $"Build number {number} ({source}).");
        }

        private static IReadOnlyList<string> ResolveScenes(FrameworkBuildProfile profile)
        {
            var scenes = new List<string>();
            if (profile.Scenes.Count > 0)
            {
                foreach (string scene in profile.Scenes)
                {
                    scenes.Add(scene);
                }

                return scenes;
            }

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            return scenes;
        }
    }
}
