using UnityEditor;
using UnityEditor.Build;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// The complete, explicit list of project settings the pipeline may change for a build:
    /// <list type="bullet">
    /// <item>application version;</item>
    /// <item>platform build number;</item>
    /// <item>application id (only when the profile overrides it);</item>
    /// <item>the Android App Bundle flag;</item>
    /// <item>Android release signing (only when CI supplies it through environment variables).</item>
    /// </list>
    ///
    /// <b>Only a setting whose value actually differs is written, and only a written setting is
    /// restored.</b> Writing a Unity setting back to its "own" value is not a no-op. Setting the
    /// application id materializes an explicit per-platform id and flips
    /// <c>overrideDefaultApplicationIdentifier</c>; setting an empty keystore name serializes as
    /// <c>'{inproject}: '</c>. An unconditional restore would therefore dirty ProjectSettings.asset
    /// even for a build that changed nothing. Restore runs in a <c>finally</c> block.
    ///
    /// The one opt-out is the profile's <c>PersistVersionChanges</c>, which deliberately keeps the
    /// version and build number. Scripting defines never appear here; they go through
    /// <c>BuildPlayerOptions.extraScriptingDefines</c>.
    ///
    /// If the Editor process itself is killed mid-build, the restore cannot run; the changed values are
    /// unsaved in-memory Player Settings that Git would show if the project were saved afterwards.
    /// </summary>
    internal sealed class TemporaryBuildSettings
    {
        private NamedBuildTarget _namedTarget;

        private bool _versionChanged;
        private string _bundleVersion;

        private bool _androidVersionCodeChanged;
        private int _androidVersionCode;

        private bool _iosBuildNumberChanged;
        private string _iosBuildNumber;

        private bool _applicationIdentifierChanged;
        private string _applicationIdentifier;

        private bool _appBundleChanged;
        private bool _buildAppBundle;

        private bool _signingChanged;
        private bool _useCustomKeystore;
        private string _keystoreName;
        private string _keystorePass;
        private string _keyaliasName;
        private string _keyaliasPass;

        public void Apply(BuildContext context)
        {
            _namedTarget = NamedBuildTarget.FromBuildTargetGroup(context.TargetGroup);

            if (PlayerSettings.bundleVersion != context.VersionText)
            {
                _bundleVersion = PlayerSettings.bundleVersion;
                _versionChanged = true;
                PlayerSettings.bundleVersion = context.VersionText;
            }

            if (context.Target == BuildTarget.Android)
            {
                if (PlayerSettings.Android.bundleVersionCode != context.BuildNumber)
                {
                    _androidVersionCode = PlayerSettings.Android.bundleVersionCode;
                    _androidVersionCodeChanged = true;
                    PlayerSettings.Android.bundleVersionCode = context.BuildNumber;
                }

                if (EditorUserBuildSettings.buildAppBundle != context.Profile.AndroidAppBundle)
                {
                    _buildAppBundle = EditorUserBuildSettings.buildAppBundle;
                    _appBundleChanged = true;
                    EditorUserBuildSettings.buildAppBundle = context.Profile.AndroidAppBundle;
                }

                ApplyAndroidSigning(context);
            }
            else if (context.Target == BuildTarget.iOS)
            {
                string buildNumber = context.BuildNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (PlayerSettings.iOS.buildNumber != buildNumber)
                {
                    _iosBuildNumber = PlayerSettings.iOS.buildNumber;
                    _iosBuildNumberChanged = true;
                    PlayerSettings.iOS.buildNumber = buildNumber;
                }
            }

            // Only an explicit profile override is applied - never a re-write of the current id.
            string current = PlayerSettings.GetApplicationIdentifier(_namedTarget);
            if (!string.IsNullOrEmpty(context.Profile.ApplicationIdentifierOverride) &&
                (context.Target == BuildTarget.Android || context.Target == BuildTarget.iOS) &&
                current != context.ApplicationIdentifier)
            {
                _applicationIdentifier = current;
                _applicationIdentifierChanged = true;
                PlayerSettings.SetApplicationIdentifier(_namedTarget, context.ApplicationIdentifier);
            }
        }

        public void Restore(bool keepVersion)
        {
            if (!keepVersion)
            {
                if (_versionChanged)
                {
                    PlayerSettings.bundleVersion = _bundleVersion;
                }

                if (_androidVersionCodeChanged)
                {
                    PlayerSettings.Android.bundleVersionCode = _androidVersionCode;
                }

                if (_iosBuildNumberChanged)
                {
                    PlayerSettings.iOS.buildNumber = _iosBuildNumber;
                }
            }

            if (_applicationIdentifierChanged)
            {
                PlayerSettings.SetApplicationIdentifier(_namedTarget, _applicationIdentifier);
            }

            if (_appBundleChanged)
            {
                EditorUserBuildSettings.buildAppBundle = _buildAppBundle;
            }

            if (_signingChanged)
            {
                PlayerSettings.Android.useCustomKeystore = _useCustomKeystore;
                PlayerSettings.Android.keystoreName = _keystoreName;
                PlayerSettings.Android.keystorePass = _keystorePass;
                PlayerSettings.Android.keyaliasName = _keyaliasName;
                PlayerSettings.Android.keyaliasPass = _keyaliasPass;
            }

            // Drop captured passwords as soon as they are no longer needed; make Restore idempotent.
            _keystorePass = null;
            _keyaliasPass = null;
            _versionChanged = _androidVersionCodeChanged = _iosBuildNumberChanged = false;
            _applicationIdentifierChanged = _appBundleChanged = _signingChanged = false;
        }

        private void ApplyAndroidSigning(BuildContext context)
        {
            AndroidSigning signing = AndroidSigning.Resolve(context.Profile, context.ProjectRoot);
            if (signing.Source != AndroidSigningSource.EnvironmentVariables)
            {
                return; // Player Settings signing (or debug signing for development) stays untouched.
            }

            _useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            _keystoreName = PlayerSettings.Android.keystoreName;
            _keystorePass = PlayerSettings.Android.keystorePass;
            _keyaliasName = PlayerSettings.Android.keyaliasName;
            _keyaliasPass = PlayerSettings.Android.keyaliasPass;
            _signingChanged = true;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = System.IO.Path.GetFullPath(signing.KeystorePath);
            PlayerSettings.Android.keystorePass = signing.KeystorePassword;
            PlayerSettings.Android.keyaliasName = signing.KeyAlias;
            PlayerSettings.Android.keyaliasPass = signing.KeyAliasPassword;
        }
    }
}
