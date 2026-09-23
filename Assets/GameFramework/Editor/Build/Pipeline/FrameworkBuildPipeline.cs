using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameFramework.Editor.Build
{
    /// <summary>The outcome of one pipeline run. Every file path is null when that file was not written.</summary>
    public sealed class BuildRunResult
    {
        public BuildRunStatus Status { get; internal set; }
        public BuildContext Context { get; internal set; }
        public BuildValidationReport Validation { get; internal set; }
        public BuildValidationReport PostBuildValidation { get; internal set; }
        public BuildReport UnityReport { get; internal set; }
        public string ArtifactPath { get; internal set; }
        public string MetadataPath { get; internal set; }
        public string ReleaseManifestPath { get; internal set; }
        public string ReportPath { get; internal set; }

        /// <summary>For an <see cref="BuildRunStatus.InvalidRequest"/> or an unexpected exception.</summary>
        public string FailureMessage { get; internal set; }

        public bool Succeeded => Status == BuildRunStatus.Succeeded || Status == BuildRunStatus.ValidationPassed;
    }

    /// <summary>
    /// Build orchestration on top of Unity's own <c>BuildPipeline.BuildPlayer</c>. It is not a
    /// separate build engine. Sequence:
    /// <code>
    /// resolve profile/version/output → capture Git → scan build scenes → preflight validators
    ///   → (stop on any Error) → apply temporary settings → BuildPlayer → restore settings
    ///   → post-build validation → {artifact}.build.json / .release-manifest.json / .build-report.json
    /// </code>
    /// Deterministic: artifact names and paths derive only from the profile and the resolved
    /// version/build number, timestamps are UTC, scripting defines are passed per build rather than
    /// written to Player Settings, and every temporarily changed setting is restored in a
    /// <c>finally</c> block. It never deletes anything; an existing artifact at the exact same path is
    /// a validation error unless the request explicitly allows overwriting it.
    /// </summary>
    public sealed class FrameworkBuildPipeline
    {
        public const string MetadataSuffix = ".build.json";
        public const string ReleaseManifestSuffix = ".release-manifest.json";
        public const string ReportSuffix = ".build-report.json";

        /// <summary>Google Play's base-module download limit; larger Android artifacts get a warning.</summary>
        private const long AndroidSizeWarningBytes = 150L * 1024 * 1024;
        private const int LargestAssetCount = 10;
        private const string LogPrefix = "[GameFramework.Build] ";

        private readonly List<IBuildValidator> _validators = new List<IBuildValidator>();

        /// <param name="additionalValidators">Project-specific checks run after the framework's own.</param>
        public FrameworkBuildPipeline(IEnumerable<IBuildValidator> additionalValidators = null)
        {
            _validators.AddRange(CreateDefaultValidators());
            if (additionalValidators != null)
            {
                _validators.AddRange(additionalValidators);
            }
        }

        public static List<IBuildValidator> CreateDefaultValidators() => new List<IBuildValidator>
        {
            new ProfileValidator(),
            new VersionHistoryValidator(),
            new SceneValidator(),
            new PlayerSettingsValidator(),
            new AndroidBuildValidator(),
            new IosBuildValidator(),
            new ReleaseSafetyValidator(),
            new FrameworkIntegrationValidator()
        };

        /// <summary>Preflight only, without side effects: no files are written and nothing is built.</summary>
        public BuildRunResult Validate(BuildRequest request)
        {
            var result = new BuildRunResult();
            if (!Prepare(request, result))
            {
                return result;
            }

            result.Status = result.Validation.HasErrors ? BuildRunStatus.ValidationFailed : BuildRunStatus.ValidationPassed;
            return result;
        }

        /// <summary>Validate, then (unless <see cref="BuildRequest.ValidateOnly"/>) build and write
        /// metadata. Always writes <c>.build-report.json</c> once a profile was resolved.</summary>
        public BuildRunResult Run(BuildRequest request)
        {
            DateTime startedUtc = DateTime.UtcNow;
            Stopwatch stopwatch = Stopwatch.StartNew();
            var result = new BuildRunResult();

            if (!Prepare(request, result))
            {
                return result;
            }

            BuildContext context = result.Context;
            Debug.Log(LogPrefix + result.Validation.ToText($"Build Validation - {context.Profile.ProfileId}"));

            if (result.Validation.HasErrors)
            {
                result.Status = BuildRunStatus.ValidationFailed;
            }
            else if (request.ValidateOnly)
            {
                result.Status = BuildRunStatus.ValidationPassed;
            }
            else
            {
                ExecuteBuild(context, result);
            }

            WriteReport(context, result, startedUtc, stopwatch.Elapsed.TotalSeconds);
            Debug.Log($"{LogPrefix}Result: {result.Status}{(result.ArtifactPath != null ? " -> " + result.ArtifactPath : string.Empty)}");
            return result;
        }

        private bool Prepare(BuildRequest request, BuildRunResult result)
        {
            if (request?.Profile == null)
            {
                result.Status = BuildRunStatus.InvalidRequest;
                result.FailureMessage = "No build profile was supplied.";
                return false;
            }

            var report = new BuildValidationReport(request.Profile.WarningsAsErrors);
            BuildContext context = BuildContext.Resolve(request, report);
            context.Git = GitInfo.Capture(context.ProjectRoot);

            foreach (string scene in context.Scenes)
            {
                context.SceneScan.ScanFile(scene);
            }

            foreach (IBuildValidator validator in _validators)
            {
                try
                {
                    validator.Validate(context, report);
                }
                catch (Exception exception)
                {
                    // A broken validator must fail the build loudly, never be skipped silently.
                    report.Error("Validator." + validator.GetType().Name, $"Validator threw {exception.GetType().Name}: {exception.Message}");
                }
            }

            result.Context = context;
            result.Validation = report;
            return true;
        }

        private static void ExecuteBuild(BuildContext context, BuildRunResult result)
        {
            Directory.CreateDirectory(context.OutputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = ToArray(context.Scenes),
                locationPathName = context.ArtifactPath,
                target = context.Target,
                targetGroup = context.TargetGroup,
                options = (context.Profile.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None) | BuildOptions.StrictMode,
                extraScriptingDefines = ToArray(context.PipelineDefines)
            };

            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup originalGroup = BuildPipeline.GetBuildTargetGroup(originalTarget);
            var temporary = new TemporaryBuildSettings();
            bool succeeded = false;

            try
            {
                temporary.Apply(context);
                result.UnityReport = BuildPipeline.BuildPlayer(options);
                succeeded = result.UnityReport != null && result.UnityReport.summary.result == BuildResult.Succeeded;
            }
            finally
            {
                bool keepVersion = succeeded && context.Profile.PersistVersionChanges;
                temporary.Restore(keepVersion);
                if (keepVersion)
                {
                    AssetDatabase.SaveAssets();
                }

                // BuildPlayer switches the active target and leaves it switched. In the Editor, switch
                // back so an interactive build leaves the project as it found it. CI passes -buildTarget,
                // so there the target never changes.
                if (!Application.isBatchMode && EditorUserBuildSettings.activeBuildTarget != originalTarget)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(originalGroup, originalTarget);
                }
            }

            if (!succeeded)
            {
                result.Status = BuildRunStatus.BuildFailed;
                result.FailureMessage = result.UnityReport != null
                    ? $"Unity build result: {result.UnityReport.summary.result} ({result.UnityReport.summary.totalErrors} error(s))."
                    : "BuildPipeline.BuildPlayer returned no report.";
                return;
            }

            result.ArtifactPath = context.ArtifactPath;
            long size = ArtifactSize(context);

            var metadata = CreateMetadata(context, size);
            result.MetadataPath = context.SidecarPath(MetadataSuffix);
            File.WriteAllText(result.MetadataPath, JsonUtility.ToJson(metadata, true));

            if (context.IsReleaseEnvironment && !context.Profile.DevelopmentBuild)
            {
                result.ReleaseManifestPath = context.SidecarPath(ReleaseManifestSuffix);
                File.WriteAllText(result.ReleaseManifestPath, JsonUtility.ToJson(CreateManifest(metadata), true));
            }

            result.PostBuildValidation = PostBuildValidate(context, result, size);
            result.Status = result.PostBuildValidation.HasErrors ? BuildRunStatus.PostBuildValidationFailed : BuildRunStatus.Succeeded;
            Debug.Log(LogPrefix + result.PostBuildValidation.ToText("Post-Build Validation"));
        }

        /// <summary>Checks the produced output against what was requested. It does not inspect the
        /// binary itself.</summary>
        internal static BuildValidationReport PostBuildValidate(BuildContext context, BuildRunResult result, long artifactSize)
        {
            var report = new BuildValidationReport(context.Profile.WarningsAsErrors);

            bool exists = File.Exists(context.ArtifactPath) || Directory.Exists(context.ArtifactPath);
            if (!exists)
            {
                report.Error("PostBuild.Artifact", $"Expected artifact not found at {context.ArtifactPath}.");
            }
            else if (artifactSize <= 0)
            {
                report.Error("PostBuild.Artifact", "The artifact is empty.");
            }
            else
            {
                report.Pass("PostBuild.Artifact", $"{Path.GetFileName(context.ArtifactPath)} ({FormatBytes(artifactSize)}).");
            }

            if (context.Target == BuildTarget.Android && artifactSize > AndroidSizeWarningBytes)
            {
                report.Warning("PostBuild.Size", $"Android artifact is {FormatBytes(artifactSize)}, above Google Play's 150 MB base-module limit.",
                    "Use Play Asset Delivery or reduce content size.");
            }

            BuildMetadata written = result.MetadataPath != null && File.Exists(result.MetadataPath)
                ? JsonUtility.FromJson<BuildMetadata>(File.ReadAllText(result.MetadataPath))
                : null;

            if (written == null)
            {
                report.Error("PostBuild.Metadata", "Build metadata was not written.");
            }
            else if (written.ApplicationVersion != context.VersionText || written.BuildNumber != context.BuildNumber ||
                     written.Target != context.Target.ToString() || written.Environment != context.Environment.ToString())
            {
                report.Error("PostBuild.Metadata", "Written metadata does not match the requested version/build number/target/environment.");
            }
            else
            {
                report.Pass("PostBuild.Metadata", $"{Path.GetFileName(result.MetadataPath)} matches the request.");
            }

            foreach (string debugOutput in FindDebugOnlyOutputs(ArtifactOwnFolder(context) ?? context.OutputDirectory, recursive: ArtifactOwnFolder(context) != null))
            {
                report.Info("PostBuild.DebugSymbols",
                    $"Debug-only output '{debugOutput}' was produced beside the artifact: archive it with the release for crash symbolication, never ship or upload it as part of the app.");
            }

            if (result.UnityReport != null && result.UnityReport.summary.totalWarnings > 0)
            {
                report.Info("PostBuild.UnityWarnings", $"Unity reported {result.UnityReport.summary.totalWarnings} build warning(s); see the Editor/CI log.");
            }

            return report;
        }

        /// <summary>Unity marks debug-only build output with a <c>_DoNotShip</c> suffix (e.g. Burst
        /// debug information, IL2CPP backup folders). Returned as names relative to
        /// <paramref name="outputDirectory"/>. Standalone players get their own artifact folder, so it is
        /// searched recursively; mobile artifacts share the environment folder, so only its top level is
        /// searched and other builds' subfolders are never attributed to this one.</summary>
        public static List<string> FindDebugOnlyOutputs(string outputDirectory, bool recursive = false)
        {
            var found = new List<string>();
            if (!Directory.Exists(outputDirectory))
            {
                return found;
            }

            foreach (string entry in Directory.GetFileSystemEntries(outputDirectory, "*_DoNotShip*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                found.Add(entry.Substring(outputDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }

            found.Sort(StringComparer.Ordinal);
            return found;
        }

        internal static BuildMetadata CreateMetadata(BuildContext context, long artifactSize) => new BuildMetadata
        {
            ProfileId = context.Profile.ProfileId,
            ApplicationVersion = context.VersionText,
            BuildNumber = context.BuildNumber,
            ApplicationIdentifier = context.ApplicationIdentifier,
            Target = context.Target.ToString(),
            Environment = context.Environment.ToString(),
            Configuration = context.Profile.DevelopmentBuild ? "Development" : "Release",
            DevelopmentBuild = context.Profile.DevelopmentBuild,
            GitCommit = context.Git.Commit,
            GitBranch = context.Git.Branch,
            GitDirty = context.Git.IsDirty,
            GitAvailable = context.Git.Available,
            BuildTimestampUtc = FormatUtc(DateTime.UtcNow),
            UnityVersion = Application.unityVersion,
            FrameworkVersion = GameFramework.Runtime.FrameworkVersion.Version,
            BuildHostPlatform = SystemInfo.operatingSystemFamily.ToString(),
            ArtifactFileName = Path.GetFileName(context.ArtifactPath),
            ArtifactSizeBytes = artifactSize,
            ScriptingDefines = ToArray(context.PipelineDefines),
            Scenes = ToArray(context.Scenes)
        };

        internal static ReleaseManifest CreateManifest(BuildMetadata metadata) => new ReleaseManifest
        {
            ApplicationVersion = metadata.ApplicationVersion,
            BuildNumber = metadata.BuildNumber,
            ApplicationIdentifier = metadata.ApplicationIdentifier,
            Target = metadata.Target,
            Environment = metadata.Environment,
            GitCommit = metadata.GitCommit,
            GitBranch = metadata.GitBranch,
            UnityVersion = metadata.UnityVersion,
            FrameworkVersion = metadata.FrameworkVersion,
            BuildTimestampUtc = metadata.BuildTimestampUtc,
            Artifact = metadata.ArtifactFileName,
            ArtifactSizeBytes = metadata.ArtifactSizeBytes
        };

        /// <summary>ISO-8601 UTC with a trailing Z, independent of the machine's culture and timezone.</summary>
        public static string FormatUtc(DateTime utc) =>
            utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        private static void WriteReport(BuildContext context, BuildRunResult result, DateTime startedUtc, double durationSeconds)
        {
            var file = new BuildReportFile
            {
                Status = result.Status.ToString(),
                ProfileId = context.Profile.ProfileId,
                Target = context.Target.ToString(),
                Environment = context.Environment.ToString(),
                ApplicationVersion = context.VersionText,
                BuildNumber = context.BuildNumber,
                ArtifactPath = result.ArtifactPath ?? string.Empty,
                ArtifactSizeBytes = result.ArtifactPath != null ? ArtifactSize(context) : 0,
                StartedUtc = FormatUtc(startedUtc),
                DurationSeconds = Math.Round(durationSeconds, 2),
                UnityBuildResult = result.UnityReport != null ? result.UnityReport.summary.result.ToString() : "NotRun",
                UnityWarningCount = result.UnityReport != null ? result.UnityReport.summary.totalWarnings : 0,
                UnityErrorCount = result.UnityReport != null ? result.UnityReport.summary.totalErrors : 0,
                GitCommit = context.Git.Commit,
                GitBranch = context.Git.Branch,
                GitDirty = context.Git.IsDirty,
                ValidationErrors = result.Validation.ErrorCount,
                ValidationWarnings = result.Validation.WarningCount,
                Validation = new List<BuildValidationIssue>(result.Validation.Issues),
                PostBuildValidation = result.PostBuildValidation != null
                    ? new List<BuildValidationIssue>(result.PostBuildValidation.Issues)
                    : new List<BuildValidationIssue>(),
                LargestAssets = LargestAssets(result.UnityReport)
            };

            try
            {
                Directory.CreateDirectory(context.OutputDirectory);
                string name = context.ArtifactBaseName.Length > 0
                    ? context.ArtifactBaseName
                    : ArtifactNaming.Sanitize(context.Profile.ProfileId) + "_invalid";
                result.ReportPath = Path.Combine(context.OutputDirectory, name + ReportSuffix);
                File.WriteAllText(result.ReportPath, JsonUtility.ToJson(file, true));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // The run's outcome is already decided and logged; losing the JSON copy must not change it.
                Debug.LogError($"{LogPrefix}Could not write the build report: {exception.Message}");
                result.ReportPath = null;
            }
        }

        private static List<BuildSizeContributor> LargestAssets(BuildReport report)
        {
            var all = new List<BuildSizeContributor>();
            if (report == null)
            {
                return all;
            }

            foreach (PackedAssets packed in report.packedAssets)
            {
                foreach (PackedAssetInfo info in packed.contents)
                {
                    all.Add(new BuildSizeContributor { AssetPath = info.sourceAssetPath, PackedSizeBytes = (long)info.packedSize });
                }
            }

            all.Sort((a, b) => b.PackedSizeBytes.CompareTo(a.PackedSizeBytes));
            if (all.Count > LargestAssetCount)
            {
                all.RemoveRange(LargestAssetCount, all.Count - LargestAssetCount);
            }

            return all;
        }

        /// <summary>The artifact's own folder for standalone players (<c>{base}/{base}.exe</c>), else null.</summary>
        private static string ArtifactOwnFolder(BuildContext context)
        {
            string folder = Path.GetDirectoryName(context.ArtifactPath);
            return folder != null && Path.GetFileName(folder) == context.ArtifactBaseName ? folder : null;
        }

        private static long ArtifactSize(BuildContext context)
        {
            string path = context.ArtifactPath;
            if (File.Exists(path))
            {
                // Standalone players: count the whole player folder, not just the launcher executable.
                string folder = Path.GetDirectoryName(path);
                bool ownFolder = folder != null && Path.GetFileName(folder) == context.ArtifactBaseName;
                return ownFolder ? DirectorySize(folder) : new FileInfo(path).Length;
            }

            return Directory.Exists(path) ? DirectorySize(path) : 0;
        }

        private static long DirectorySize(string directory)
        {
            long total = 0;
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                total += new FileInfo(file).Length;
            }

            return total;
        }

        private static string FormatBytes(long bytes) =>
            (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";

        private static string[] ToArray(IReadOnlyList<string> values)
        {
            var array = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                array[i] = values[i];
            }

            return array;
        }
    }
}
