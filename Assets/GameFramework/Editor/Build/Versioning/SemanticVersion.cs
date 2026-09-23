using System;
using System.Globalization;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// The application version in the only form both stores accept: one to three non-negative
    /// integers separated by dots (<c>1</c>, <c>1.4</c>, <c>1.4.0</c>). iOS requires exactly this for
    /// <c>CFBundleShortVersionString</c>. Android's <c>versionName</c> is free text, but the framework
    /// keeps one rule for both platforms so a version string never means different things per store.
    /// Pre-release/metadata suffixes (<c>-beta</c>, <c>+sha</c>) are rejected for the same reason; put
    /// that information in the build metadata instead.
    /// </summary>
    public readonly struct SemanticVersion : IEquatable<SemanticVersion>, IComparable<SemanticVersion>
    {
        /// <summary>Upper bound per component, well above any real version and small enough that
        /// the encoded build-number scheme can reason about overflow.</summary>
        public const int MaxComponent = 99999;

        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;

        /// <summary>How many components the source text had (1-3). Preserved so formatting
        /// round-trips exactly: "1.0" stays "1.0".</summary>
        public readonly int ComponentCount;

        public SemanticVersion(int major, int minor, int patch, int componentCount = 3)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            ComponentCount = componentCount < 1 ? 1 : componentCount > 3 ? 3 : componentCount;
        }

        public static bool TryParse(string text, out SemanticVersion version, out string error)
        {
            version = default;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Version is empty.";
                return false;
            }

            if (text.Trim() != text)
            {
                error = $"Version '{text}' has leading/trailing whitespace.";
                return false;
            }

            string[] parts = text.Split('.');
            if (parts.Length > 3)
            {
                error = $"Version '{text}' has more than three components; stores accept MAJOR[.MINOR[.PATCH]].";
                return false;
            }

            var values = new int[3];
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0 || !IsAllDigits(part))
                {
                    error = $"Version '{text}' must contain only digits separated by dots (component '{part}').";
                    return false;
                }

                if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value > MaxComponent)
                {
                    error = $"Version component '{part}' in '{text}' exceeds {MaxComponent}.";
                    return false;
                }

                values[i] = value;
            }

            version = new SemanticVersion(values[0], values[1], values[2], parts.Length);
            error = null;
            return true;
        }

        public override string ToString()
        {
            switch (ComponentCount)
            {
                case 1: return Major.ToString(CultureInfo.InvariantCulture);
                case 2: return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Major, Minor);
                default: return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch);
            }
        }

        public int CompareTo(SemanticVersion other)
        {
            int result = Major.CompareTo(other.Major);
            if (result != 0)
            {
                return result;
            }

            result = Minor.CompareTo(other.Minor);
            return result != 0 ? result : Patch.CompareTo(other.Patch);
        }

        public bool Equals(SemanticVersion other) => Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        public override bool Equals(object obj) => obj is SemanticVersion other && Equals(other);
        public override int GetHashCode() => (Major * 397 ^ Minor) * 397 ^ Patch;

        private static bool IsAllDigits(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
