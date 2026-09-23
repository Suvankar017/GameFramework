using System;
using System.Collections.Generic;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// <c>{artifact}.build.json</c>: written beside every successful artifact. It is serialized with
    /// <c>JsonUtility</c> (the framework's existing serialization convention) and carries an explicit
    /// <see cref="SchemaVersion"/>. It contains no secrets, no environment variables, and no user or
    /// machine names; timestamps are UTC ISO-8601.
    /// </summary>
    [Serializable]
    public sealed class BuildMetadata
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string ProfileId;
        public string ApplicationVersion;
        public int BuildNumber;
        public string ApplicationIdentifier;
        public string Target;
        public string Environment;

        /// <summary>"Development" (Unity Development Build) or "Release".</summary>
        public string Configuration;
        public bool DevelopmentBuild;
        public string GitCommit;
        public string GitBranch;
        public bool GitDirty;
        public bool GitAvailable;
        public string BuildTimestampUtc;
        public string UnityVersion;
        public string FrameworkVersion;

        /// <summary>The OS family the build ran on (e.g. "Windows"), never a machine name.</summary>
        public string BuildHostPlatform;
        public string ArtifactFileName;
        public long ArtifactSizeBytes;
        public string[] ScriptingDefines = Array.Empty<string>();
        public string[] Scenes = Array.Empty<string>();
    }

    /// <summary>
    /// <c>{artifact}.release-manifest.json</c>: written only for successful non-development builds
    /// in the Staging/Production environments, i.e. the artifacts that can actually be released. It is
    /// the small, stable subset a release process or store upload needs; <see cref="BuildMetadata"/>
    /// has the full detail.
    /// </summary>
    [Serializable]
    public sealed class ReleaseManifest
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string ApplicationVersion;
        public int BuildNumber;
        public string ApplicationIdentifier;
        public string Target;
        public string Environment;
        public string GitCommit;
        public string GitBranch;
        public string UnityVersion;
        public string FrameworkVersion;
        public string BuildTimestampUtc;
        public string Artifact;
        public long ArtifactSizeBytes;
    }

    [Serializable]
    public sealed class BuildSizeContributor
    {
        public string AssetPath;
        public long PackedSizeBytes;
    }

    /// <summary>
    /// <c>{artifact}.build-report.json</c>: the machine-readable outcome of every pipeline run that
    /// resolved a profile, including runs that failed validation or failed to build. CI can read
    /// <see cref="Status"/> instead of scraping logs.
    /// </summary>
    [Serializable]
    public sealed class BuildReportFile
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;

        /// <summary>One of <see cref="BuildRunStatus"/>'s names.</summary>
        public string Status;
        public string ProfileId;
        public string Target;
        public string Environment;
        public string ApplicationVersion;
        public int BuildNumber;
        public string ArtifactPath;
        public long ArtifactSizeBytes;
        public string StartedUtc;
        public double DurationSeconds;
        public string UnityBuildResult;
        public int UnityWarningCount;
        public int UnityErrorCount;
        public string GitCommit;
        public string GitBranch;
        public bool GitDirty;
        public int ValidationErrors;
        public int ValidationWarnings;
        public List<BuildValidationIssue> Validation = new List<BuildValidationIssue>();
        public List<BuildValidationIssue> PostBuildValidation = new List<BuildValidationIssue>();
        public List<BuildSizeContributor> LargestAssets = new List<BuildSizeContributor>();
    }

    public enum BuildRunStatus
    {
        Succeeded,
        ValidationPassed,
        InvalidRequest,
        ValidationFailed,
        BuildFailed,
        PostBuildValidationFailed
    }
}
