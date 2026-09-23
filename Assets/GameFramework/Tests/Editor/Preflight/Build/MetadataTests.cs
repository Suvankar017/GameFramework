using System;
using System.IO;
using GameFramework.Editor.Build;
using GameFramework.Runtime;
using GameFramework.Runtime.Security;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Editor.Tests.Build
{
    public class MetadataTests
    {
        private FrameworkBuildProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<FrameworkBuildProfile>();
            _profile.ProfileId = "AndroidProduction";
            _profile.Target = BuildTarget.Android;
            _profile.Environment = DeploymentEnvironment.Production;
            _profile.DevelopmentBuild = false;
            _profile.VersionOverride = "1.4.0";
            _profile.BuildNumberScheme = BuildNumberScheme.EncodedFromVersion;
            _profile.ArtifactSlug = "Game";
            _profile.ExtraDefines = new[] { "GAME_FEATURE" };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_profile);

        private BuildContext Resolve() => BuildContext.Resolve(new BuildRequest(_profile), new BuildValidationReport());

        [Test]
        public void Metadata_CarriesVersionTargetEnvironmentAndFrameworkVersion()
        {
            BuildMetadata metadata = FrameworkBuildPipeline.CreateMetadata(Resolve(), 1234);

            Assert.AreEqual(BuildMetadata.CurrentSchemaVersion, metadata.SchemaVersion);
            Assert.AreEqual("1.4.0", metadata.ApplicationVersion);
            Assert.AreEqual(10400, metadata.BuildNumber);
            Assert.AreEqual("Android", metadata.Target);
            Assert.AreEqual("Production", metadata.Environment);
            Assert.AreEqual("Release", metadata.Configuration);
            Assert.AreEqual(FrameworkVersion.Version, metadata.FrameworkVersion);
            Assert.AreEqual(Application.unityVersion, metadata.UnityVersion);
            Assert.AreEqual("Game_Android_Production_1.4.0_10400.apk", metadata.ArtifactFileName);
            Assert.AreEqual(1234, metadata.ArtifactSizeBytes);
            CollectionAssert.AreEqual(new[] { "GAMEFRAMEWORK_ENV_PRODUCTION", "GAME_FEATURE" }, metadata.ScriptingDefines);
        }

        [Test]
        public void Metadata_GitUnavailable_UsesExplicitFallback()
        {
            BuildContext context = Resolve();
            context.Git = GitInfo.Unavailable("test");

            BuildMetadata metadata = FrameworkBuildPipeline.CreateMetadata(context, 1);

            Assert.AreEqual(GitInfo.Unknown, metadata.GitCommit);
            Assert.AreEqual(GitInfo.Unknown, metadata.GitBranch);
            Assert.IsFalse(metadata.GitAvailable);
        }

        [Test]
        public void Metadata_TimestampIsUtcIso8601()
        {
            BuildMetadata metadata = FrameworkBuildPipeline.CreateMetadata(Resolve(), 1);

            StringAssert.IsMatch(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", metadata.BuildTimestampUtc);
        }

        [Test]
        public void FormatUtc_ConvertsLocalTimeAndIgnoresCulture()
        {
            var utc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            Assert.AreEqual("2026-01-02T03:04:05Z", FrameworkBuildPipeline.FormatUtc(utc));
            Assert.AreEqual("2026-01-02T03:04:05Z", FrameworkBuildPipeline.FormatUtc(utc.ToLocalTime()));
        }

        [Test]
        public void Manifest_IsSubsetOfMetadata()
        {
            BuildMetadata metadata = FrameworkBuildPipeline.CreateMetadata(Resolve(), 99);

            ReleaseManifest manifest = FrameworkBuildPipeline.CreateManifest(metadata);

            Assert.AreEqual(ReleaseManifest.CurrentSchemaVersion, manifest.SchemaVersion);
            Assert.AreEqual(metadata.ApplicationVersion, manifest.ApplicationVersion);
            Assert.AreEqual(metadata.BuildNumber, manifest.BuildNumber);
            Assert.AreEqual(metadata.Target, manifest.Target);
            Assert.AreEqual(metadata.Environment, manifest.Environment);
            Assert.AreEqual(metadata.GitCommit, manifest.GitCommit);
            Assert.AreEqual(metadata.ArtifactFileName, manifest.Artifact);
        }

        [Test]
        public void Metadata_JsonRoundTrip()
        {
            BuildMetadata metadata = FrameworkBuildPipeline.CreateMetadata(Resolve(), 5);

            BuildMetadata read = JsonUtility.FromJson<BuildMetadata>(JsonUtility.ToJson(metadata, true));

            Assert.AreEqual(metadata.ApplicationVersion, read.ApplicationVersion);
            Assert.AreEqual(metadata.BuildNumber, read.BuildNumber);
            CollectionAssert.AreEqual(metadata.Scenes, read.Scenes);
        }

        [Test]
        public void Metadata_ContainsNoEnvironmentVariablesOrUserNames()
        {
            string json = JsonUtility.ToJson(FrameworkBuildPipeline.CreateMetadata(Resolve(), 1));

            StringAssert.DoesNotContain(Environment.UserName, json);
            StringAssert.DoesNotContain(Environment.MachineName, json);
        }

        [Test]
        public void Git_OutsideRepository_ReportsUnavailable()
        {
            string directory = Path.Combine(Path.GetTempPath(), "GameFrameworkGitTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                GitInfo git = GitInfo.Capture(directory);

                Assert.IsFalse(git.Available);
                Assert.AreEqual(GitInfo.Unknown, git.Commit);
                Assert.IsNotEmpty(git.FailureReason);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Git_ThisRepository_CapturesCommitAndBranch()
        {
            GitInfo git = GitInfo.Capture(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));

            if (!git.Available)
            {
                Assert.Ignore("Git is not installed or this project is not a Git repository on this machine: " + git.FailureReason);
            }

            StringAssert.IsMatch("^[0-9a-f]{40}$", git.Commit);
            Assert.IsNotEmpty(git.Branch);
        }

        [Test]
        public void FindDebugOnlyOutputs_ReportsDoNotShipEntries()
        {
            string directory = Path.Combine(Path.GetTempPath(), "GameFrameworkDebugOutput_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(directory, "Game_BurstDebugInformation_DoNotShip"));
            File.WriteAllText(Path.Combine(directory, "Game.apk"), "x");
            try
            {
                var found = FrameworkBuildPipeline.FindDebugOnlyOutputs(directory);

                CollectionAssert.AreEqual(new[] { "Game_BurstDebugInformation_DoNotShip" }, found);
                Assert.AreEqual(0, FrameworkBuildPipeline.FindDebugOnlyOutputs(Path.Combine(directory, "missing")).Count);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void AndroidSigning_MissingVariables_NamedButValuesNeverExposed()
        {
            AndroidSigning signing = AndroidSigning.Resolve(_profile, Path.GetTempPath());

            if (signing.Source == AndroidSigningSource.EnvironmentVariables)
            {
                Assert.Ignore("Signing variables are set on this machine.");
            }

            Assert.Contains(_profile.AndroidKeystorePasswordVariable, signing.MissingVariables);
        }
    }
}
