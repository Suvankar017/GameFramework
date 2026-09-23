using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GameFramework.DeepLinks;
using GameFramework.Notifications;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// Android preflight: versionCode, SDK levels, scripting backend/architecture, App Bundle,
    /// release signing (see <see cref="AndroidSigning"/>), custom manifest/Gradle templates, and
    /// deep-link/notification manifest requirements. It validates what is observable from the Unity
    /// project; it never edits the manifest or Player Settings.
    /// </summary>
    public sealed class AndroidBuildValidator : IBuildValidator
    {
        private const string CustomManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        private static readonly Regex PermissionPattern = new Regex("<uses-permission[^>]*android:name=\"([^\"]+)\"");
        private static readonly Regex SchemePattern = new Regex("<data[^>]*android:scheme=\"([^\"]+)\"");

        public void Validate(BuildContext context, BuildValidationReport report)
        {
            if (context.Target != BuildTarget.Android)
            {
                return;
            }

            if (context.BuildNumber > 0)
            {
                report.Pass("Android.VersionCode", $"versionCode {context.BuildNumber}, versionName {context.VersionText}.");
            }

            ValidateSdkLevels(report);
            ValidateArchitecture(context, report);
            ValidateSigning(context, report);
            ValidateManifest(context, report);

            if (context.IsReleaseEnvironment)
            {
                report.Info("Android.Symbols", EditorUserBuildSettings.androidCreateSymbols == AndroidCreateSymbols.Disabled
                    ? "Native debug symbols are not generated; Play Console crash reports for native code will not be symbolicated. Consider Build Settings > Create symbols.zip (Public)."
                    : $"Native symbols: {EditorUserBuildSettings.androidCreateSymbols}. Upload symbols.zip to Play Console with the release; keep it out of the app.");
            }

            if (context.IsProduction && !context.Profile.AndroidAppBundle)
            {
                report.Warning("Android.AppBundle", "Production profile builds an APK. Google Play requires an Android App Bundle (.aab) for new apps.",
                    "Tick 'Android App Bundle' on the Production profile.");
            }
        }

        private static void ValidateSdkLevels(BuildValidationReport report)
        {
            int min = (int)PlayerSettings.Android.minSdkVersion;
            int target = (int)PlayerSettings.Android.targetSdkVersion;

            if (target != 0 && target < min)
            {
                report.Error("Android.SdkLevels", $"Target API level {target} is below the minimum API level {min}.");
                return;
            }

            report.Info("Android.SdkLevels", target == 0
                ? $"Minimum API {min}; target API = highest installed (Automatic). Store target-API requirements change yearly; check Google Play's current requirement."
                : $"Minimum API {min}; target API {target}. Check Google Play's current target-API requirement.");
        }

        private static void ValidateArchitecture(BuildContext context, BuildValidationReport report)
        {
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            bool arm64 = (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;

            if (!context.Profile.AndroidRequireArm64)
            {
                report.Info("Android.Architecture", $"Backend {backend}, architectures {PlayerSettings.Android.targetArchitectures} (ARM64 not required by this profile).");
                return;
            }

            bool ok = true;
            if (backend != ScriptingImplementation.IL2CPP)
            {
                report.ErrorOrWarning(context.IsReleaseEnvironment, "Android.Architecture",
                    $"Scripting backend is {backend}; ARM64 (required by Google Play for 64-bit support) needs IL2CPP.",
                    "Player Settings > Other Settings > Scripting Backend = IL2CPP.");
                ok = false;
            }

            if (!arm64)
            {
                report.ErrorOrWarning(context.IsReleaseEnvironment, "Android.Architecture",
                    $"Target architectures are {PlayerSettings.Android.targetArchitectures}; ARM64 is not included.",
                    "Player Settings > Other Settings > Target Architectures: tick ARM64.");
                ok = false;
            }

            if (ok)
            {
                report.Pass("Android.Architecture", $"IL2CPP with {PlayerSettings.Android.targetArchitectures}.");
            }
        }

        private static void ValidateSigning(BuildContext context, BuildValidationReport report)
        {
            AndroidSigning signing = AndroidSigning.Resolve(context.Profile, context.ProjectRoot);

            if (!context.IsReleaseEnvironment && context.Profile.DevelopmentBuild)
            {
                report.Info("Android.Signing", signing.Source == AndroidSigningSource.None
                    ? "Development build: Unity's debug keystore will be used."
                    : $"Release keystore available ({signing.Source}).");
                return;
            }

            switch (signing.Source)
            {
                case AndroidSigningSource.None:
                    report.ErrorOrWarning(context.IsProduction, "Android.Signing",
                        $"No release keystore: set the environment variables {string.Join(", ", signing.MissingVariables)} (values are never logged). Without them Unity would silently sign with the debug key.",
                        "Provide the keystore through your CI secret store; never commit the keystore or passwords.");
                    return;
                case AndroidSigningSource.EnvironmentVariables:
                case AndroidSigningSource.PlayerSettings:
                    if (signing.KeystoreFileMissing)
                    {
                        report.Error("Android.Signing", $"The release keystore file referenced by {signing.Source} does not exist.");
                        return;
                    }

                    if (signing.KeystoreInsideProject)
                    {
                        report.Warning("Android.KeystoreLocation", "The release keystore is inside the project folder.",
                            "Keep keystores outside the repository (or at minimum git-ignored) and inject them in CI.");
                    }

                    report.Pass("Android.Signing", $"Release signing configured from {signing.Source}.");
                    return;
            }
        }

        private static void ValidateManifest(BuildContext context, BuildValidationReport report)
        {
            string manifest = File.Exists(CustomManifestPath) ? File.ReadAllText(CustomManifestPath) : null;
            if (manifest != null)
            {
                var permissions = new List<string>();
                foreach (Match match in PermissionPattern.Matches(manifest))
                {
                    permissions.Add(match.Groups[1].Value);
                }

                report.Info("Android.Manifest", $"Custom AndroidManifest.xml present; declared permissions: {(permissions.Count == 0 ? "none" : string.Join(", ", permissions))}.");
            }

            foreach (string template in new[] { "mainTemplate.gradle", "launcherTemplate.gradle", "baseProjectTemplate.gradle", "gradleTemplate.properties" })
            {
                if (File.Exists(Path.Combine("Assets/Plugins/Android", template)))
                {
                    report.Info("Android.Gradle", $"Custom Gradle template in use: {template}.");
                }
            }

            SceneComponentScanner scan = context.SceneScan;
            if (Any<DeepLinksBootstrapper>(scan))
            {
                if (manifest == null || !SchemePattern.IsMatch(manifest))
                {
                    report.Warning("Android.DeepLinks", "The build contains a DeepLinksBootstrapper, but no custom AndroidManifest.xml declares an intent-filter <data android:scheme=...>; custom-scheme/app links will not open the app.",
                        $"Add an intent-filter to {CustomManifestPath} (Publishing Settings > Custom Main Manifest).");
                }
                else
                {
                    report.Pass("Android.DeepLinks", "Deep-link intent-filter scheme declared in the custom manifest.");
                }
            }

            if (Any<NotificationsBootstrapper>(scan))
            {
                bool postNotifications = manifest != null && manifest.IndexOf("android.permission.POST_NOTIFICATIONS", StringComparison.Ordinal) >= 0;
                report.Info("Android.Notifications", postNotifications
                    ? "POST_NOTIFICATIONS permission declared."
                    : "The build contains a NotificationsBootstrapper. On Android 13+ a real notification provider needs android.permission.POST_NOTIFICATIONS (usually added by the provider's own package).");
            }
        }

        internal static bool Any<T>(SceneComponentScanner scan)
        {
            foreach (ScannedComponent unused in scan.OfType<T>())
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// iOS preflight for what the Unity project controls: build number format, deployment target,
    /// URL schemes and deep-link/notification capability reminders. Signing is handled one of two
    /// ways. <b>External</b> (the default): signing is completed in Xcode/CI, so Unity does not need a
    /// team or profile. <b>Unity-managed</b>: Unity's signing settings must be complete. Capabilities
    /// and entitlements live in the generated Xcode project and are reported as reminders, not validated.
    /// </summary>
    public sealed class IosBuildValidator : IBuildValidator
    {
        public void Validate(BuildContext context, BuildValidationReport report)
        {
            if (context.Target != BuildTarget.iOS)
            {
                return;
            }

            if (context.BuildNumber > 0)
            {
                report.Pass("iOS.BuildNumber", $"CFBundleVersion {context.BuildNumber}, CFBundleShortVersionString {context.VersionText}.");
            }

            string deploymentTarget = PlayerSettings.iOS.targetOSVersionString;
            if (!Version.TryParse(deploymentTarget.Contains(".") ? deploymentTarget : deploymentTarget + ".0", out Version parsedTarget))
            {
                report.Error("iOS.DeploymentTarget", $"Deployment target '{deploymentTarget}' is not a version number.");
            }
            else
            {
                report.Info("iOS.DeploymentTarget", $"Minimum iOS {parsedTarget}.");
            }

            ValidateSigning(context, report);

            SceneComponentScanner scan = context.SceneScan;
            if (AndroidBuildValidator.Any<DeepLinksBootstrapper>(scan))
            {
                string[] schemes = PlayerSettings.iOS.iOSUrlSchemes;
                if (schemes == null || schemes.Length == 0)
                {
                    report.Warning("iOS.DeepLinks", "The build contains a DeepLinksBootstrapper but no iOS URL scheme is configured; custom-scheme links will not open the app.",
                        "Player Settings > iOS > Other Settings > Supported URL schemes. Universal links additionally need the Associated Domains capability in Xcode.");
                }
                else
                {
                    report.Pass("iOS.DeepLinks", $"URL scheme(s): {string.Join(", ", schemes)}. Universal links (if used) need the Associated Domains capability in Xcode.");
                }
            }

            if (AndroidBuildValidator.Any<NotificationsBootstrapper>(scan))
            {
                report.Info("iOS.Notifications", "The build contains a NotificationsBootstrapper: remote notifications need the Push Notifications capability/entitlement in the Xcode project (not observable from Unity).");
            }
        }

        private static void ValidateSigning(BuildContext context, BuildValidationReport report)
        {
            if (context.Profile.IosSigningHandledExternally)
            {
                report.Info("iOS.Signing", "Signing is completed in Xcode/CI (profile setting). Unity-side validation only; certificates and provisioning stay outside the repository.");
                return;
            }

            bool hasTeam = !string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID);
            bool automatic = PlayerSettings.iOS.appleEnableAutomaticSigning;
            bool hasManualProfile = !string.IsNullOrEmpty(PlayerSettings.iOS.iOSManualProvisioningProfileID);

            if (!hasTeam || (!automatic && !hasManualProfile))
            {
                report.ErrorOrWarning(context.IsReleaseEnvironment, "iOS.Signing",
                    "Unity-managed signing is incomplete: a Team ID plus automatic signing or a manual provisioning profile is required.",
                    "Complete Player Settings > iOS > Identification, or tick 'iOS Signing Handled Externally' if Xcode/CI signs.");
                return;
            }

            report.Pass("iOS.Signing", automatic ? "Automatic signing with a Team ID." : "Manual provisioning profile configured.");
        }
    }
}
