using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Security;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// One pre-build check. It adds structured results to the report and never throws for a failed
    /// check. A game can add its own project-specific validators by passing them to
    /// <see cref="FrameworkBuildPipeline"/>'s constructor.
    /// </summary>
    public interface IBuildValidator
    {
        void Validate(BuildContext context, BuildValidationReport report);
    }

    /// <summary>Profile/request consistency: id, environment, development flag, artifact naming,
    /// output location, and "never overwrite an existing artifact implicitly".</summary>
    public sealed class ProfileValidator : IBuildValidator
    {
        private static readonly Regex ProfileIdPattern = new Regex("^[A-Za-z0-9_-]+$");

        public void Validate(BuildContext context, BuildValidationReport report)
        {
            FrameworkBuildProfile profile = context.Profile;

            if (string.IsNullOrEmpty(profile.ProfileId) || !ProfileIdPattern.IsMatch(profile.ProfileId))
            {
                report.Error("Profile.Id", $"Profile id '{profile.ProfileId}' must be non-empty and use only letters, digits, '-' and '_'.");
            }
            else
            {
                report.Pass("Profile.Id", $"Profile '{profile.ProfileId}' ({ArtifactNaming.PlatformName(context.Target)} + {context.Environment}).");
            }

            if (context.Environment == DeploymentEnvironment.Unspecified)
            {
                report.Error("Profile.Environment", "The profile has no environment. Choose Development, Staging, or Production explicitly.");
            }

            if (context.IsProduction && profile.DevelopmentBuild)
            {
                report.Error("Profile.DevelopmentBuild", "Production profiles must not produce a Unity Development Build.",
                    "Untick 'Development Build' on the profile, or use a Staging/Development profile.");
            }
            else if (profile.DevelopmentBuild)
            {
                report.Info("Profile.DevelopmentBuild", "Development Build: profiler, script debugging and development-only providers are enabled.");
            }

            if (!BuildPipeline.IsBuildTargetSupported(context.TargetGroup, context.Target))
            {
                report.Error("Profile.Target", $"Build support for {context.Target} is not installed in this Unity editor.",
                    "Install the platform module through Unity Hub.");
            }

            foreach (string define in context.PipelineDefines)
            {
                if (!BuildDefines.IsValidSymbol(define))
                {
                    report.Error("Defines.Format", $"'{define}' is not a valid scripting define symbol.");
                }
            }

            ValidateOutput(context, report);
        }

        private static void ValidateOutput(BuildContext context, BuildValidationReport report)
        {
            if (context.ArtifactBaseName.Length == 0)
            {
                if (context.VersionValid)
                {
                    report.Error("Output.ArtifactName", "The artifact name is empty: set the profile's Artifact Slug or a Product Name with letters/digits.");
                }

                return;
            }

            string assets = Path.GetFullPath(Application.dataPath);
            if (context.OutputDirectory.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
            {
                report.Error("Output.Location", "The output directory is inside Assets/; Unity would import the build output.",
                    "Use the default 'Builds' root (git-ignored) or a path outside the project.");
            }

            if (File.Exists(context.ArtifactPath) || Directory.Exists(context.ArtifactPath))
            {
                report.ErrorOrWarning(!context.Request.AllowOverwrite, "Output.Exists",
                    $"An artifact already exists at {context.ArtifactPath} (same version and build number).",
                    "Bump the build number, or pass -overwrite / tick Overwrite to replace it deliberately.");
            }
            else
            {
                report.Pass("Output.ArtifactName", $"Artifact {Path.GetFileName(context.ArtifactPath)}.");
            }
        }
    }

    /// <summary>Build-number monotonicity against the project and previous artifacts in the same
    /// output folder (stores reject a build number that doesn't increase).</summary>
    public sealed class VersionHistoryValidator : IBuildValidator
    {
        public void Validate(BuildContext context, BuildValidationReport report)
        {
            if (context.BuildNumber <= 0)
            {
                return; // Resolution already reported the problem.
            }

            int highest = HighestPreviousBuildNumber(context.OutputDirectory, context.ArtifactBaseName);
            if (highest > 0 && context.BuildNumber < highest)
            {
                report.ErrorOrWarning(context.IsProduction, "Version.Monotonic",
                    $"Build number {context.BuildNumber} is lower than {highest}, already built into {context.OutputDirectory}.",
                    "Stores reject a lower build number than one already uploaded; increase it.");
            }
            else
            {
                report.Pass("Version.Monotonic", highest > 0
                    ? $"Build number {context.BuildNumber} >= previous {highest} in this output folder."
                    : "No previous build metadata in this output folder.");
            }
        }

        /// <summary>Highest <c>BuildNumber</c> in any <c>*.build.json</c> of the folder, excluding the
        /// artifact being rebuilt. Unreadable files are skipped.</summary>
        public static int HighestPreviousBuildNumber(string directory, string excludeBaseName)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            int highest = 0;
            foreach (string file in Directory.GetFiles(directory, "*" + FrameworkBuildPipeline.MetadataSuffix))
            {
                if (!string.IsNullOrEmpty(excludeBaseName) &&
                    Path.GetFileName(file).Equals(excludeBaseName + FrameworkBuildPipeline.MetadataSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var metadata = JsonUtility.FromJson<BuildMetadata>(File.ReadAllText(file));
                    if (metadata != null && metadata.BuildNumber > highest)
                    {
                        highest = metadata.BuildNumber;
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is ArgumentException)
                {
                    // A hand-edited/partial metadata file must not break validation of a new build.
                    continue;
                }
            }

            return highest;
        }
    }

    /// <summary>Scene list: non-empty, existing SceneAssets, no duplicates, text-serialized for
    /// scanning, no missing scripts, and (optionally) a GameBootstrapper in the first scene.</summary>
    public sealed class SceneValidator : IBuildValidator
    {
        public void Validate(BuildContext context, BuildValidationReport report)
        {
            if (context.Scenes.Count == 0)
            {
                report.Error("Scenes.Empty", "No scenes to build (the profile lists none and Build Settings has no enabled scenes).");
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool allValid = true;
            foreach (string scene in context.Scenes)
            {
                if (string.IsNullOrWhiteSpace(scene) || !scene.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    report.Error("Scenes.Path", $"'{scene}' is not a scene path (Assets/.../Name.unity).");
                    allValid = false;
                }
                else if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                {
                    report.Error("Scenes.Missing", $"Scene '{scene}' does not exist.", "Fix the path in the profile or Build Settings.");
                    allValid = false;
                }

                if (!string.IsNullOrWhiteSpace(scene) && !seen.Add(scene))
                {
                    report.Error("Scenes.Duplicate", $"Scene '{scene}' is listed more than once.");
                    allValid = false;
                }
            }

            if (allValid)
            {
                report.Pass("Scenes.List", $"{context.Scenes.Count} scene(s); startup scene {context.Scenes[0]}.");
            }

            SceneComponentScanner scan = context.SceneScan;
            foreach (string file in scan.UnscannableFiles)
            {
                report.Warning("Scenes.Scannable", $"'{file}' is not text-serialized; its components could not be validated.",
                    "Set Editor Settings > Asset Serialization to Force Text.");
            }

            foreach ((string source, string guid) in scan.MissingScripts)
            {
                report.ErrorOrWarning(context.IsReleaseEnvironment, "Scenes.MissingScript",
                    $"'{source}' has a component whose script is missing (guid {guid}).", "Remove or restore the missing script.");
            }

            if (context.Profile.RequireBootstrapperInFirstScene && allValid)
            {
                bool found = false;
                foreach (ScannedComponent component in scan.OfType<GameBootstrapper>())
                {
                    found |= string.Equals(component.RootScenePath, context.Scenes[0], StringComparison.OrdinalIgnoreCase);
                }

                if (found)
                {
                    report.Pass("Scenes.Bootstrapper", "The startup scene contains a GameBootstrapper.");
                }
                else
                {
                    report.Error("Scenes.Bootstrapper", $"The startup scene '{context.Scenes[0]}' has no GameBootstrapper (or subclass), placed directly or through a prefab instance.",
                        "Add your bootstrapper to the first scene, or untick 'Require Bootstrapper In First Scene' on the profile.");
                }
            }
        }
    }

    /// <summary>Cross-platform Player Settings: company/product names, application id format and
    /// placeholders, icons, and whether Addressables needs attention. It reports dangerous or clearly
    /// invalid values only; it never dictates project-specific choices.</summary>
    public sealed class PlayerSettingsValidator : IBuildValidator
    {
        private static readonly Regex ApplicationIdPattern = new Regex("^[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)+$");
        private static readonly string[] PlaceholderFragments = { "DefaultCompany", "com.example", "com.company", "com.yourcompany", "Placeholder", "changeme" };

        public void Validate(BuildContext context, BuildValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) || ContainsPlaceholder(PlayerSettings.companyName))
            {
                report.ErrorOrWarning(context.IsProduction, "PlayerSettings.CompanyName", $"Company Name is '{PlayerSettings.companyName}' (empty or a placeholder).");
            }
            else
            {
                report.Pass("PlayerSettings.CompanyName", "Company Name set.");
            }

            if (string.IsNullOrWhiteSpace(PlayerSettings.productName))
            {
                report.Error("PlayerSettings.ProductName", "Product Name is empty.");
            }

            ValidateApplicationIdentifier(context, report);
            ValidateIcons(context, report);
            ValidateAddressables(report);
        }

        public static bool IsValidApplicationIdentifier(string identifier) =>
            !string.IsNullOrEmpty(identifier) && identifier.Length <= 150 && ApplicationIdPattern.IsMatch(identifier);

        public static bool ContainsPlaceholder(string value)
        {
            foreach (string fragment in PlaceholderFragments)
            {
                if (value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateApplicationIdentifier(BuildContext context, BuildValidationReport report)
        {
            if (context.Target != BuildTarget.Android && context.Target != BuildTarget.iOS)
            {
                return;
            }

            string id = context.ApplicationIdentifier;
            if (!IsValidApplicationIdentifier(id))
            {
                report.Error("PlayerSettings.ApplicationId", $"Application id '{id}' is not a valid reverse-domain identifier (e.g. com.company.game).");
                return;
            }

            if (ContainsPlaceholder(id))
            {
                report.ErrorOrWarning(context.IsReleaseEnvironment, "PlayerSettings.ApplicationId",
                    $"Application id '{id}' looks like a placeholder.", "Set the real package/bundle id (optionally per environment via the profile).");
                return;
            }

            report.Pass("PlayerSettings.ApplicationId", $"Application id {id}.");
        }

        private static void ValidateIcons(BuildContext context, BuildValidationReport report)
        {
            Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            bool hasDefault = false;
            if (icons != null)
            {
                foreach (Texture2D icon in icons)
                {
                    hasDefault |= icon != null;
                }
            }

            if (hasDefault)
            {
                report.Pass("PlayerSettings.Icon", "Default application icon assigned.");
            }
            else if (context.IsReleaseEnvironment && (context.Target == BuildTarget.Android || context.Target == BuildTarget.iOS))
            {
                report.Warning("PlayerSettings.Icon", "No default application icon is assigned; the store build would ship Unity's placeholder icon.",
                    "Assign an icon in Player Settings > Icon.");
            }
        }

        private static void ValidateAddressables(BuildValidationReport report)
        {
            string manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
            bool usesAddressables = File.Exists(manifestPath) &&
                                    File.ReadAllText(manifestPath).IndexOf("\"com.unity.addressables\"", StringComparison.Ordinal) >= 0;

            if (usesAddressables)
            {
                report.Warning("Content.Addressables",
                    "com.unity.addressables is installed. This pipeline does not build Addressables content; build it for the target/profile before the player build.",
                    "Run your Addressables content build step (e.g. AddressableAssetSettings.BuildPlayerContent) in CI before invoking this pipeline.");
            }
            else
            {
                report.Info("Content.Addressables", "Addressables not installed; no content build required.");
            }
        }
    }
}
