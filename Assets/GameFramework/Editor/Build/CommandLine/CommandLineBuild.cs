using System;
using GameFramework.Runtime.Security;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// CI entry point. Provider-neutral: any CI system that can run Unity works.
    /// <code>
    /// Unity -batchmode -quit -nographics -projectPath &lt;project&gt; -logFile -
    ///       -buildTarget Android
    ///       -executeMethod GameFramework.Editor.Build.CommandLineBuild.Build
    ///       -profile AndroidProduction [-environment Production] [-version 1.4.0] [-buildNumber 10400]
    ///       [-outputPath Builds] [-validateOnly] [-overwrite]
    /// </code>
    /// The process always ends through <see cref="EditorApplication.Exit"/> with one of the
    /// <see cref="ExitCode"/> values, so CI never sees success after a failed preflight or build.
    /// </summary>
    public static class CommandLineBuild
    {
        public enum ExitCode
        {
            Success = 0,
            UnexpectedError = 1,
            InvalidArguments = 2,
            ValidationFailed = 3,
            BuildFailed = 4,
            PostBuildValidationFailed = 5
        }

        private const string LogPrefix = "[GameFramework.Build] ";

        /// <summary>The <c>-executeMethod</c> target.</summary>
        public static void Build()
        {
            ExitCode code;
            try
            {
                code = Execute(Environment.GetCommandLineArgs());
            }
            catch (Exception exception)
            {
                // Logged in full and turned into a failing exit code - never swallowed.
                Debug.LogException(exception);
                code = ExitCode.UnexpectedError;
            }

            Debug.Log($"{LogPrefix}Exit code {(int)code} ({code}).");
            EditorApplication.Exit((int)code);
        }

        /// <summary>Everything except the process exit, so it can be tested without quitting.</summary>
        public static ExitCode Execute(string[] commandLine)
        {
            CommandLineArguments args = CommandLineArguments.Parse(commandLine);
            if (!args.IsValid)
            {
                Debug.LogError(LogPrefix + "Invalid arguments:\n  " + string.Join("\n  ", args.Errors));
                return ExitCode.InvalidArguments;
            }

            if (!TryCreateRequest(args, out BuildRequest request, out string error))
            {
                Debug.LogError(LogPrefix + error);
                return ExitCode.InvalidArguments;
            }

            BuildRunResult result = new FrameworkBuildPipeline().Run(request);
            return ToExitCode(result);
        }

        public static ExitCode ToExitCode(BuildRunResult result)
        {
            switch (result.Status)
            {
                case BuildRunStatus.Succeeded:
                case BuildRunStatus.ValidationPassed:
                    return ExitCode.Success;
                case BuildRunStatus.InvalidRequest:
                    return ExitCode.InvalidArguments;
                case BuildRunStatus.ValidationFailed:
                    return ExitCode.ValidationFailed;
                case BuildRunStatus.BuildFailed:
                    return ExitCode.BuildFailed;
                case BuildRunStatus.PostBuildValidationFailed:
                    return ExitCode.PostBuildValidationFailed;
                default:
                    return ExitCode.UnexpectedError;
            }
        }

        /// <summary>Accepts both <see cref="BuildTarget"/> names and the aliases Unity's own
        /// <c>-buildTarget</c> command-line flag uses (Win64, Win, OSXUniversal, Linux64).</summary>
        public static bool TryParseBuildTarget(string value, out BuildTarget target)
        {
            switch ((value ?? string.Empty).ToLowerInvariant())
            {
                case "win64": target = BuildTarget.StandaloneWindows64; return true;
                case "win": target = BuildTarget.StandaloneWindows; return true;
                case "osxuniversal": target = BuildTarget.StandaloneOSX; return true;
                case "linux64": target = BuildTarget.StandaloneLinux64; return true;
            }

            // Reject numeric strings: Enum.TryParse accepts "3", which is never a meaningful target name.
            return Enum.TryParse(value, true, out target) && Enum.IsDefined(typeof(BuildTarget), target) &&
                   !char.IsDigit(value[0]);
        }

        /// <summary>Resolves the profile and checks the optional assertions (-environment,
        /// -buildTarget) against it.</summary>
        public static bool TryCreateRequest(CommandLineArguments args, out BuildRequest request, out string error)
        {
            request = null;
            if (!FrameworkBuildProfile.TryFind(args.Profile, out FrameworkBuildProfile profile, out error))
            {
                return false;
            }

            return TryCreateRequest(args, profile, out request, out error);
        }

        /// <summary>Profile-supplied overload (unit-testable without assets on disk).</summary>
        public static bool TryCreateRequest(CommandLineArguments args, FrameworkBuildProfile profile, out BuildRequest request, out string error)
        {
            request = null;

            if (args.Environment != null)
            {
                if (!Enum.TryParse(args.Environment, true, out DeploymentEnvironment environment) ||
                    environment == DeploymentEnvironment.Unspecified || !Enum.IsDefined(typeof(DeploymentEnvironment), environment))
                {
                    error = $"-environment '{args.Environment}' is not one of Development, Staging, Production.";
                    return false;
                }

                if (environment != profile.Environment)
                {
                    error = $"-environment {environment} does not match profile '{profile.ProfileId}' ({profile.Environment}). Use the profile for that environment.";
                    return false;
                }
            }

            if (args.BuildTarget != null)
            {
                if (!TryParseBuildTarget(args.BuildTarget, out BuildTarget target))
                {
                    error = $"-buildTarget '{args.BuildTarget}' is not a Unity BuildTarget.";
                    return false;
                }

                if (target != profile.Target)
                {
                    error = $"-buildTarget {target} does not match profile '{profile.ProfileId}' ({profile.Target}).";
                    return false;
                }
            }

            request = new BuildRequest(profile)
            {
                VersionOverride = args.Version,
                BuildNumberOverride = args.BuildNumber,
                OutputRootOverride = args.OutputPath,
                ValidateOnly = args.ValidateOnly,
                AllowOverwrite = args.Overwrite
            };

            error = null;
            return true;
        }
    }
}
