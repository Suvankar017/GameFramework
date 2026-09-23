using System;
using System.Collections.Generic;
using GameFramework.Runtime.Security;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// One named, versioned build configuration: <b>target platform</b> × <b>deployment environment</b>
    /// (e.g. "AndroidStaging", "AndroidProduction", "iOSProduction"). Editor-only data; a profile is
    /// never referenced by a scene, so it never ships in a player.
    ///
    /// <para>The profile holds only what genuinely varies between builds. Everything else (icons,
    /// orientation, graphics APIs, ...) stays in Unity's Player Settings, where the build pipeline
    /// <i>validates</i> it but never rewrites it. The few values a profile can change for a build
    /// (application version, build number, application id, Android signing from environment
    /// variables) are applied temporarily and restored afterwards; see
    /// <see cref="FrameworkBuildPipeline"/>.</para>
    ///
    /// <para>Contains no secrets. Signing credentials are referenced only by the <i>names</i> of the
    /// environment variables that CI populates.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "BuildProfile", menuName = "GameFramework/Build/Build Profile")]
    public sealed class FrameworkBuildProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id used by the command line (-profile). Letters, digits, '-', '_' only.")]
        [SerializeField] private string _profileId = "AndroidDevelopment";

        [SerializeField] private BuildTarget _target = BuildTarget.Android;
        [SerializeField] private DeploymentEnvironment _environment = DeploymentEnvironment.Development;

        [Tooltip("Unity 'Development Build' (profiler, script debugging, DEVELOPMENT_BUILD). Never allowed for Production.")]
        [SerializeField] private bool _developmentBuild = true;

        [Header("Content")]
        [Tooltip("Scenes to build, first = startup scene. Empty = the enabled scenes in Build Settings.")]
        [SerializeField] private string[] _scenes = Array.Empty<string>();

        [Tooltip("Require a GameBootstrapper (or subclass) in the first scene.")]
        [SerializeField] private bool _requireBootstrapperInFirstScene = true;

        [Header("Versioning")]
        [Tooltip("Empty = PlayerSettings.bundleVersion. -version on the command line overrides both.")]
        [SerializeField] private string _versionOverride = string.Empty;

        [SerializeField] private BuildNumberScheme _buildNumberScheme = BuildNumberScheme.FromPlayerSettings;

        [Tooltip("Keep the resolved version/build number in Player Settings after the build. Off = restore the originals.")]
        [SerializeField] private bool _persistVersionChanges;

        [Header("Identifiers & Output")]
        [Tooltip("Optional per-environment application id (e.g. com.company.game.dev). Empty = Player Settings.")]
        [SerializeField] private string _applicationIdentifierOverride = string.Empty;

        [Tooltip("Artifact name prefix. Empty = sanitized Product Name.")]
        [SerializeField] private string _artifactSlug = string.Empty;

        [Tooltip("Relative to the project root unless absolute. The default 'Builds' folder is git-ignored.")]
        [SerializeField] private string _outputRoot = "Builds";

        [Header("Scripting Defines (build-only; Player Settings are not modified)")]
        [SerializeField] private string[] _extraDefines = Array.Empty<string>();

        [Tooltip("Additional define names that must not be present in a Production build.")]
        [SerializeField] private string[] _forbiddenProductionDefines = Array.Empty<string>();

        [Header("Release Safety")]
        [Tooltip("Fail when the Git working tree has uncommitted changes (recommended for Production).")]
        [SerializeField] private bool _requireCleanGitTree;

        [Tooltip("Validation check ids whose warnings should fail this profile's builds.")]
        [SerializeField] private string[] _warningsAsErrors = Array.Empty<string>();

        [Header("Android")]
        [SerializeField] private bool _androidAppBundle;
        [SerializeField] private bool _androidRequireArm64 = true;

        [Tooltip("Environment variable names CI sets for release signing. Values are never logged or stored.")]
        [SerializeField] private string _androidKeystorePathVariable = "GF_ANDROID_KEYSTORE_PATH";
        [SerializeField] private string _androidKeystorePasswordVariable = "GF_ANDROID_KEYSTORE_PASSWORD";
        [SerializeField] private string _androidKeyAliasVariable = "GF_ANDROID_KEY_ALIAS";
        [SerializeField] private string _androidKeyAliasPasswordVariable = "GF_ANDROID_KEY_ALIAS_PASSWORD";

        [Header("iOS")]
        [Tooltip("On (default): code signing is completed in Xcode/CI, so Unity only validates the project. Off: Unity signing settings must be complete.")]
        [SerializeField] private bool _iosSigningHandledExternally = true;

        public string ProfileId { get => _profileId; internal set => _profileId = value; }
        public BuildTarget Target { get => _target; internal set => _target = value; }
        public DeploymentEnvironment Environment { get => _environment; internal set => _environment = value; }
        public bool DevelopmentBuild { get => _developmentBuild; internal set => _developmentBuild = value; }
        public IReadOnlyList<string> Scenes { get => _scenes; internal set => _scenes = ToArray(value); }
        public bool RequireBootstrapperInFirstScene { get => _requireBootstrapperInFirstScene; internal set => _requireBootstrapperInFirstScene = value; }
        public string VersionOverride { get => _versionOverride; internal set => _versionOverride = value; }
        public BuildNumberScheme BuildNumberScheme { get => _buildNumberScheme; internal set => _buildNumberScheme = value; }
        public bool PersistVersionChanges { get => _persistVersionChanges; internal set => _persistVersionChanges = value; }
        public string ApplicationIdentifierOverride { get => _applicationIdentifierOverride; internal set => _applicationIdentifierOverride = value; }
        public string ArtifactSlug { get => _artifactSlug; internal set => _artifactSlug = value; }
        public string OutputRoot { get => _outputRoot; internal set => _outputRoot = value; }
        public IReadOnlyList<string> ExtraDefines { get => _extraDefines; internal set => _extraDefines = ToArray(value); }
        public IReadOnlyList<string> ForbiddenProductionDefines { get => _forbiddenProductionDefines; internal set => _forbiddenProductionDefines = ToArray(value); }
        public bool RequireCleanGitTree { get => _requireCleanGitTree; internal set => _requireCleanGitTree = value; }
        public IReadOnlyList<string> WarningsAsErrors { get => _warningsAsErrors; internal set => _warningsAsErrors = ToArray(value); }
        public bool AndroidAppBundle { get => _androidAppBundle; internal set => _androidAppBundle = value; }
        public bool AndroidRequireArm64 { get => _androidRequireArm64; internal set => _androidRequireArm64 = value; }
        public string AndroidKeystorePathVariable => _androidKeystorePathVariable;
        public string AndroidKeystorePasswordVariable => _androidKeystorePasswordVariable;
        public string AndroidKeyAliasVariable => _androidKeyAliasVariable;
        public string AndroidKeyAliasPasswordVariable => _androidKeyAliasPasswordVariable;
        public bool IosSigningHandledExternally { get => _iosSigningHandledExternally; internal set => _iosSigningHandledExternally = value; }

        /// <summary>Every profile asset in the project. Explicit scan, called on demand only.</summary>
        public static List<FrameworkBuildProfile> FindAll()
        {
            var profiles = new List<FrameworkBuildProfile>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FrameworkBuildProfile)))
            {
                var profile = AssetDatabase.LoadAssetAtPath<FrameworkBuildProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            profiles.Sort((a, b) => string.CompareOrdinal(a.ProfileId, b.ProfileId));
            return profiles;
        }

        /// <summary>Finds a profile by <see cref="ProfileId"/>. Fails (with a reason) on no match or on
        /// an ambiguous duplicate id, never picks one arbitrarily.</summary>
        public static bool TryFind(string profileId, out FrameworkBuildProfile profile, out string error)
        {
            profile = null;
            foreach (FrameworkBuildProfile candidate in FindAll())
            {
                if (!string.Equals(candidate.ProfileId, profileId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (profile != null)
                {
                    error = $"Profile id '{profileId}' is used by more than one asset ({AssetDatabase.GetAssetPath(profile)}, {AssetDatabase.GetAssetPath(candidate)}).";
                    profile = null;
                    return false;
                }

                profile = candidate;
            }

            error = profile == null ? $"No build profile with id '{profileId}' exists." : null;
            return profile != null;
        }

        private static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null)
            {
                return Array.Empty<string>();
            }

            var array = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                array[i] = values[i];
            }

            return array;
        }
    }
}
