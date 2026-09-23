using System;
using System.Collections.Generic;
using System.Globalization;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// Parses the pipeline's own options out of a Unity command line. Unity passes every argument
    /// through (<c>-batchmode</c>, <c>-projectPath</c>, <c>-logFile</c>, ...), so only the options below
    /// are interpreted and everything else is ignored:
    /// <code>
    /// -profile &lt;id&gt;          required  FrameworkBuildProfile.ProfileId
    /// -environment &lt;name&gt;    optional  must equal the profile's environment (an assertion, not an override)
    /// -buildTarget &lt;name&gt;    optional  Unity's own flag; must equal the profile's target
    /// -outputPath &lt;dir&gt;      optional  output root (Platform/Environment are still appended)
    /// -version &lt;x.y.z&gt;       optional  application version override
    /// -buildNumber &lt;n&gt;       optional  platform build number override
    /// -validateOnly            optional  run preflight only
    /// -overwrite               optional  allow replacing an identical existing artifact
    /// </code>
    /// Options are case-insensitive. A duplicate, missing value, or malformed number is an error.
    /// Values are never echoed back verbatim in errors except for these non-secret options.
    /// </summary>
    public sealed class CommandLineArguments
    {
        private static readonly string[] ValueOptions = { "-profile", "-environment", "-buildTarget", "-outputPath", "-version", "-buildNumber" };
        private static readonly string[] FlagOptions = { "-validateOnly", "-overwrite" };

        public string Profile { get; private set; }
        public string Environment { get; private set; }
        public string BuildTarget { get; private set; }
        public string OutputPath { get; private set; }
        public string Version { get; private set; }
        public int? BuildNumber { get; private set; }
        public bool ValidateOnly { get; private set; }
        public bool Overwrite { get; private set; }

        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public static CommandLineArguments Parse(IReadOnlyList<string> args)
        {
            var parsed = new CommandLineArguments();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < (args?.Count ?? 0); i++)
            {
                string option = Match(args[i], ValueOptions) ?? Match(args[i], FlagOptions);
                if (option == null)
                {
                    continue; // A Unity option or an unrelated argument.
                }

                if (!seen.Add(option))
                {
                    parsed.Errors.Add($"{option} was given more than once.");
                    continue;
                }

                if (Array.IndexOf(FlagOptions, option) >= 0)
                {
                    parsed.SetFlag(option);
                    continue;
                }

                if (i + 1 >= args.Count || args[i + 1].StartsWith("-", StringComparison.Ordinal) && !IsNegativeNumber(args[i + 1]))
                {
                    parsed.Errors.Add($"{option} requires a value.");
                    continue;
                }

                parsed.SetValue(option, args[++i]);
            }

            if (string.IsNullOrWhiteSpace(parsed.Profile) && !parsed.Errors.Exists(e => e.StartsWith("-profile", StringComparison.Ordinal)))
            {
                parsed.Errors.Add("-profile <id> is required.");
            }

            return parsed;
        }

        private void SetFlag(string option)
        {
            if (option == "-validateOnly")
            {
                ValidateOnly = true;
            }
            else
            {
                Overwrite = true;
            }
        }

        private void SetValue(string option, string value)
        {
            switch (option)
            {
                case "-profile": Profile = value; break;
                case "-environment": Environment = value; break;
                case "-buildTarget": BuildTarget = value; break;
                case "-outputPath": OutputPath = value; break;
                case "-version": Version = value; break;
                case "-buildNumber":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                    {
                        BuildNumber = number;
                    }
                    else
                    {
                        Errors.Add($"-buildNumber '{value}' is not an integer.");
                    }

                    break;
            }
        }

        private static string Match(string argument, string[] options)
        {
            foreach (string option in options)
            {
                if (string.Equals(argument, option, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            return null;
        }

        private static bool IsNegativeNumber(string value) =>
            value.Length > 1 && value[0] == '-' && char.IsDigit(value[1]);
    }
}
