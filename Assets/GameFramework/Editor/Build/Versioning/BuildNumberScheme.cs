using System;

namespace GameFramework.Editor.Build
{
    /// <summary>How the platform build number (Android <c>versionCode</c>, iOS
    /// <c>CFBundleVersion</c>) is derived. It is chosen per build profile, and an explicit
    /// <c>-buildNumber</c> command-line value always wins over either scheme.</summary>
    public enum BuildNumberScheme
    {
        /// <summary>Use the build number already in Player Settings for the target platform
        /// (or the explicit override). The framework never invents a number.</summary>
        FromPlayerSettings,

        /// <summary><c>major * 10000 + minor * 100 + patch</c>, e.g. 1.4.0 → 10400. Requires
        /// minor and patch below 100 so two versions never map to the same number.</summary>
        EncodedFromVersion
    }

    public static class BuildNumbers
    {
        /// <summary>Google Play's documented maximum <c>versionCode</c>.</summary>
        public const int MaxAndroidVersionCode = 2100000000;

        /// <summary>Encodes <paramref name="version"/> per <see cref="BuildNumberScheme.EncodedFromVersion"/>.</summary>
        public static bool TryEncode(SemanticVersion version, out int buildNumber, out string error)
        {
            buildNumber = 0;

            if (version.Minor > 99 || version.Patch > 99)
            {
                error = $"Version {version} cannot be encoded: minor and patch must be below 100 for the major*10000+minor*100+patch scheme.";
                return false;
            }

            long encoded = (long)version.Major * 10000 + version.Minor * 100 + version.Patch;
            if (encoded < 1 || encoded > MaxAndroidVersionCode)
            {
                error = $"Version {version} encodes to {encoded}, outside the valid build number range 1..{MaxAndroidVersionCode}.";
                return false;
            }

            buildNumber = (int)encoded;
            error = null;
            return true;
        }

        /// <summary>Platform constraints shared by both stores for the integer form: 1..2,100,000,000.</summary>
        public static bool IsValid(int buildNumber, out string error)
        {
            if (buildNumber < 1 || buildNumber > MaxAndroidVersionCode)
            {
                error = $"Build number {buildNumber} is outside the valid range 1..{MaxAndroidVersionCode}.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Parses the iOS <c>CFBundleVersion</c> string Unity stores. Only a plain integer
        /// is accepted here, because the framework keeps one integer build number for both stores.</summary>
        public static bool TryParse(string text, out int buildNumber)
        {
            buildNumber = 0;
            return !string.IsNullOrEmpty(text) &&
                   int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out buildNumber);
        }

        public static int Resolve(BuildNumberScheme scheme, SemanticVersion version, int playerSettingsBuildNumber, int? explicitOverride, out string error)
        {
            if (explicitOverride.HasValue)
            {
                return IsValid(explicitOverride.Value, out error) ? explicitOverride.Value : 0;
            }

            switch (scheme)
            {
                case BuildNumberScheme.EncodedFromVersion:
                    return TryEncode(version, out int encoded, out error) ? encoded : 0;
                case BuildNumberScheme.FromPlayerSettings:
                    return IsValid(playerSettingsBuildNumber, out error) ? playerSettingsBuildNumber : 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scheme), scheme, null);
            }
        }
    }
}
