using System.Globalization;
using System.IO;
using System.Text;
using GameFramework.Runtime.Security;
using UnityEditor;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// Deterministic artifact names and output locations. The same inputs always produce the same
    /// name: no timestamps, no random parts, no machine-specific parts.
    /// <code>
    /// {Builds}/{Platform}/{Environment}/{Slug}_{Platform}_{Environment}_{Version}_{BuildNumber}{.ext}
    /// Builds/Android/Production/MyGame_Android_Production_1.4.0_10400.aab
    /// Builds/Android/Production/MyGame_Android_Production_1.4.0_10400.build.json
    /// </code>
    /// </summary>
    public static class ArtifactNaming
    {
        /// <summary>Replaces anything outside <c>[A-Za-z0-9._-]</c> with <c>-</c> (spaces, slashes,
        /// colons, unicode) and trims leading/trailing separators, so a name is safe on every
        /// filesystem and in every CI shell without quoting.</summary>
        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                bool safe = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';
                builder.Append(safe ? c : '-');
            }

            return builder.ToString().Trim('-', '.', '_');
        }

        /// <summary>Short, stable platform folder/name segment.</summary>
        public static string PlatformName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android: return "Android";
                case BuildTarget.iOS: return "iOS";
                case BuildTarget.StandaloneWindows64: return "Windows64";
                case BuildTarget.StandaloneOSX: return "macOS";
                case BuildTarget.StandaloneLinux64: return "Linux64";
                default: return Sanitize(target.ToString());
            }
        }

        /// <summary>The artifact base name (no extension). Returns empty if <paramref name="slug"/>
        /// sanitizes to nothing, which validation reports as an error.</summary>
        public static string BaseName(string slug, BuildTarget target, DeploymentEnvironment environment, SemanticVersion version, int buildNumber)
        {
            string safeSlug = Sanitize(slug);
            if (safeSlug.Length == 0)
            {
                return string.Empty;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}",
                safeSlug, PlatformName(target), environment, version, buildNumber);
        }

        /// <summary>What Unity's <c>BuildPlayerOptions.locationPathName</c> needs per target: a file
        /// for Android (.apk/.aab), a folder for the iOS Xcode project, an executable inside its own
        /// folder for standalone players (so their data folders never mix).</summary>
        public static string LocationPath(string directory, string baseName, BuildTarget target, bool androidAppBundle)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return Path.Combine(directory, baseName + (androidAppBundle ? ".aab" : ".apk"));
                case BuildTarget.iOS:
                    return Path.Combine(directory, baseName);
                case BuildTarget.StandaloneWindows64:
                    return Path.Combine(directory, baseName, baseName + ".exe");
                case BuildTarget.StandaloneOSX:
                    return Path.Combine(directory, baseName + ".app");
                case BuildTarget.StandaloneLinux64:
                    return Path.Combine(directory, baseName, baseName + ".x86_64");
                default:
                    return Path.Combine(directory, baseName, baseName);
            }
        }

        /// <summary><c>{root}/{Platform}/{Environment}</c>.</summary>
        public static string OutputDirectory(string root, BuildTarget target, DeploymentEnvironment environment) =>
            Path.Combine(root, PlatformName(target), environment.ToString());
    }
}
