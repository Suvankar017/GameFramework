using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace GameFramework.Editor.Build
{
    public enum AndroidSigningSource
    {
        /// <summary>No release keystore; Unity would sign with the debug key.</summary>
        None,

        /// <summary>CI supplied the keystore path, alias and passwords through environment variables.</summary>
        EnvironmentVariables,

        /// <summary>A custom keystore is already configured in Player Settings for this session
        /// (Unity keeps the passwords in memory only; they are never in ProjectSettings.asset).</summary>
        PlayerSettings
    }

    /// <summary>
    /// Resolves Android release signing. <b>Signing boundary:</b> the repository holds only the
    /// <i>names</i> of the environment variables (on the profile). CI injects the keystore file and
    /// the passwords from its own secret store. This class never logs, serializes, or returns a
    /// password or a keystore path in any message; it reports only which variable names are missing.
    /// </summary>
    public sealed class AndroidSigning
    {
        private AndroidSigning()
        {
        }

        public AndroidSigningSource Source { get; private set; }

        /// <summary>Names (not values) of the signing variables that are unset.</summary>
        public List<string> MissingVariables { get; } = new List<string>();

        public bool KeystoreFileMissing { get; private set; }
        public bool KeystoreInsideProject { get; private set; }

        // Values stay private and are only handed to Player Settings for the duration of a build.
        internal string KeystorePath { get; private set; }
        internal string KeystorePassword { get; private set; }
        internal string KeyAlias { get; private set; }
        internal string KeyAliasPassword { get; private set; }

        public static AndroidSigning Resolve(FrameworkBuildProfile profile, string projectRoot)
        {
            var signing = new AndroidSigning
            {
                KeystorePath = Read(profile.AndroidKeystorePathVariable),
                KeystorePassword = Read(profile.AndroidKeystorePasswordVariable),
                KeyAlias = Read(profile.AndroidKeyAliasVariable),
                KeyAliasPassword = Read(profile.AndroidKeyAliasPasswordVariable)
            };

            AddIfMissing(signing, profile.AndroidKeystorePathVariable, signing.KeystorePath);
            AddIfMissing(signing, profile.AndroidKeystorePasswordVariable, signing.KeystorePassword);
            AddIfMissing(signing, profile.AndroidKeyAliasVariable, signing.KeyAlias);
            AddIfMissing(signing, profile.AndroidKeyAliasPasswordVariable, signing.KeyAliasPassword);

            if (signing.MissingVariables.Count == 0)
            {
                signing.Source = AndroidSigningSource.EnvironmentVariables;
                string fullPath = Path.GetFullPath(signing.KeystorePath);
                signing.KeystoreFileMissing = !File.Exists(fullPath);
                signing.KeystoreInsideProject = fullPath.StartsWith(Path.GetFullPath(projectRoot), System.StringComparison.OrdinalIgnoreCase);
                return signing;
            }

            if (PlayerSettings.Android.useCustomKeystore && !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                signing.Source = AndroidSigningSource.PlayerSettings;
                signing.KeystoreFileMissing = !File.Exists(PlayerSettings.Android.keystoreName);
                signing.KeystoreInsideProject = Path.GetFullPath(PlayerSettings.Android.keystoreName)
                    .StartsWith(Path.GetFullPath(projectRoot), System.StringComparison.OrdinalIgnoreCase);
                return signing;
            }

            signing.Source = AndroidSigningSource.None;
            return signing;
        }

        private static string Read(string variableName) =>
            string.IsNullOrEmpty(variableName) ? null : System.Environment.GetEnvironmentVariable(variableName);

        private static void AddIfMissing(AndroidSigning signing, string variableName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                signing.MissingVariables.Add(string.IsNullOrEmpty(variableName) ? "<unnamed variable>" : variableName);
            }
        }
    }
}
