using System;
using System.IO;
using GameFramework.Editor.Build;
using GameFramework.RemoteConfig;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Security;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Editor.Tests.Build
{
    /// <summary>Validators run against in-memory profiles and hand-written scene YAML, so no scene or
    /// Player Settings asset is modified. BuildContext.Resolve only reads Player Settings.</summary>
    public class BuildValidationTests
    {
        private FrameworkBuildProfile _profile;
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<FrameworkBuildProfile>();
            _profile.ProfileId = "TestProfile";
            _profile.Target = BuildTarget.StandaloneWindows64;
            _profile.Environment = DeploymentEnvironment.Development;
            _profile.DevelopmentBuild = true;
            _profile.VersionOverride = "1.2.3";
            _profile.ArtifactSlug = "Test";
            _profile.RequireBootstrapperInFirstScene = false;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "GameFrameworkBuildTests_" + Guid.NewGuid().ToString("N"));
            _profile.OutputRoot = _tempDirectory;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private BuildContext Resolve(out BuildValidationReport report, BuildRequest request = null)
        {
            report = new BuildValidationReport(_profile.WarningsAsErrors);
            return BuildContext.Resolve(request ?? new BuildRequest(_profile), report);
        }

        private static bool Has(BuildValidationReport report, string checkId, BuildValidationSeverity severity)
        {
            foreach (BuildValidationIssue issue in report.Issues)
            {
                if (issue.CheckId == checkId && issue.Severity == severity)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void Report_WarningPromotedByProfile_BecomesError()
        {
            var report = new BuildValidationReport(new[] { "Android.AppBundle" });

            report.Warning("Android.AppBundle", "APK");
            report.Warning("Other", "not promoted");

            Assert.AreEqual(1, report.ErrorCount);
            Assert.AreEqual(1, report.WarningCount);
            Assert.IsTrue(report.Issues[0].Promoted);
            Assert.AreEqual("Error", report.Issues[0].Level);
        }

        [Test]
        public void Report_Text_HasCountsAndPrefixes()
        {
            var report = new BuildValidationReport();
            report.Pass("A", "ok");
            report.Warning("B", "careful", "do this");
            report.Error("C", "broken");

            string text = report.ToText("Build Validation");

            StringAssert.Contains("Errors: 1  Warnings: 1  Info: 0  Passed: 1", text);
            StringAssert.Contains("[PASS] A: ok", text);
            StringAssert.Contains("[WARN] B: careful", text);
            StringAssert.Contains("fix: do this", text);
            StringAssert.Contains("[FAIL] C: broken", text);
        }

        [Test]
        public void Resolve_InvalidVersion_ReportsErrorAndNoArtifactName()
        {
            _profile.VersionOverride = "1.0-beta";

            BuildContext context = Resolve(out BuildValidationReport report);

            Assert.IsTrue(Has(report, "Version.Format", BuildValidationSeverity.Error));
            Assert.AreEqual(string.Empty, context.ArtifactBaseName);
        }

        [Test]
        public void Resolve_RequestOverridesBeatProfile()
        {
            BuildContext context = Resolve(out _, new BuildRequest(_profile) { VersionOverride = "3.1", BuildNumberOverride = 99 });

            Assert.AreEqual("3.1", context.VersionText);
            Assert.AreEqual(99, context.BuildNumber);
            StringAssert.Contains("Test_Windows64_Development_3.1_99", context.ArtifactBaseName);
        }

        [Test]
        public void Resolve_EncodedScheme_DerivesBuildNumber()
        {
            _profile.BuildNumberScheme = BuildNumberScheme.EncodedFromVersion;

            Assert.AreEqual(10203, Resolve(out _).BuildNumber);
        }

        [Test]
        public void Resolve_PipelineDefinesIncludeEnvironment()
        {
            _profile.Environment = DeploymentEnvironment.Staging;
            _profile.ExtraDefines = new[] { "MY_FLAG" };

            CollectionAssert.AreEqual(new[] { "GAMEFRAMEWORK_ENV_STAGING", "MY_FLAG" }, Resolve(out _).PipelineDefines);
        }

        [Test]
        public void Resolve_OutputSeparatesPlatformAndEnvironment()
        {
            BuildContext context = Resolve(out _);

            StringAssert.EndsWith(Path.Combine("Windows64", "Development"), context.OutputDirectory);
            StringAssert.StartsWith(Path.GetFullPath(_tempDirectory), context.OutputDirectory);
        }

        [Test]
        public void ProfileValidator_ProductionDevelopmentBuild_IsError()
        {
            _profile.Environment = DeploymentEnvironment.Production;
            _profile.DevelopmentBuild = true;
            BuildContext context = Resolve(out BuildValidationReport report);

            new ProfileValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Profile.DevelopmentBuild", BuildValidationSeverity.Error));
        }

        [Test]
        public void ProfileValidator_UnspecifiedEnvironment_IsError()
        {
            _profile.Environment = DeploymentEnvironment.Unspecified;
            BuildContext context = Resolve(out BuildValidationReport report);

            new ProfileValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Profile.Environment", BuildValidationSeverity.Error));
        }

        [Test]
        public void ProfileValidator_InvalidExtraDefine_IsError()
        {
            _profile.ExtraDefines = new[] { "BAD-DEFINE" };
            BuildContext context = Resolve(out BuildValidationReport report);

            new ProfileValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Defines.Format", BuildValidationSeverity.Error));
        }

        [Test]
        public void ProfileValidator_ExistingArtifact_ErrorUnlessOverwriteAllowed()
        {
            BuildContext context = Resolve(out BuildValidationReport report);
            Directory.CreateDirectory(Path.GetDirectoryName(context.ArtifactPath));
            File.WriteAllText(context.ArtifactPath, "previous build");

            new ProfileValidator().Validate(context, report);
            Assert.IsTrue(Has(report, "Output.Exists", BuildValidationSeverity.Error));

            BuildContext allowed = Resolve(out BuildValidationReport allowedReport, new BuildRequest(_profile) { AllowOverwrite = true });
            new ProfileValidator().Validate(allowed, allowedReport);
            Assert.IsTrue(Has(allowedReport, "Output.Exists", BuildValidationSeverity.Warning));
        }

        [Test]
        public void SceneValidator_MissingAndDuplicateScenes_AreErrors()
        {
            _profile.Scenes = new[] { "Assets/DoesNotExist.unity", "Assets/DoesNotExist.unity", "NotAScene" };
            BuildContext context = Resolve(out BuildValidationReport report);

            new SceneValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Scenes.Missing", BuildValidationSeverity.Error));
            Assert.IsTrue(Has(report, "Scenes.Duplicate", BuildValidationSeverity.Error));
            Assert.IsTrue(Has(report, "Scenes.Path", BuildValidationSeverity.Error));
        }

        [Test]
        public void ReleaseSafety_ForbiddenDefineInProduction_IsError()
        {
            _profile.Environment = DeploymentEnvironment.Production;
            _profile.DevelopmentBuild = false;
            _profile.ExtraDefines = new[] { "ENABLE_CHEATS" };
            BuildContext context = Resolve(out BuildValidationReport report);

            new ReleaseSafetyValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Defines.Production", BuildValidationSeverity.Error));
        }

        [Test]
        public void ReleaseSafety_ForbiddenDefineInDevelopment_IsAllowed()
        {
            _profile.ExtraDefines = new[] { "ENABLE_CHEATS" };
            BuildContext context = Resolve(out BuildValidationReport report);

            new ReleaseSafetyValidator().Validate(context, report);

            Assert.IsFalse(Has(report, "Defines.Production", BuildValidationSeverity.Error));
        }

        [TestCase(DeploymentEnvironment.Production, BuildValidationSeverity.Error)]
        [TestCase(DeploymentEnvironment.Staging, BuildValidationSeverity.Warning)]
        [TestCase(DeploymentEnvironment.Development, BuildValidationSeverity.Info)]
        public void ReleaseSafety_MockProviderInScene_SeverityByEnvironment(DeploymentEnvironment environment, BuildValidationSeverity expected)
        {
            _profile.Environment = environment;
            _profile.DevelopmentBuild = environment != DeploymentEnvironment.Production;
            BuildContext context = Resolve(out BuildValidationReport report);
            context.SceneScan.ScanText("Assets/Test.unity", SceneYaml(ScriptGuid<RemoteConfigBootstrapper>(), "  _useMockProvider: 1"));

            new ReleaseSafetyValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Providers.Mock", expected));
        }

        [Test]
        public void ReleaseSafety_MockEnabledThroughPrefabOverride_IsDetected()
        {
            _profile.Environment = DeploymentEnvironment.Production;
            _profile.DevelopmentBuild = false;
            BuildContext context = Resolve(out BuildValidationReport report);
            context.SceneScan.ScanText("Assets/Test.unity",
                "%YAML 1.1\n--- !u!1001 &100\nPrefabInstance:\n  m_Modification:\n    m_Modifications:\n    - target: {fileID: 1}\n      propertyPath: _useMockAnalyticsProvider\n      value: 1\n      objectReference: {fileID: 0}\n");

            new ReleaseSafetyValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Providers.Mock", BuildValidationSeverity.Error));
        }

        [Test]
        public void ReleaseSafety_RequireCleanTree_GitUnavailableIsError()
        {
            _profile.RequireCleanGitTree = true;
            BuildContext context = Resolve(out BuildValidationReport report);
            context.Git = GitInfo.Unavailable("test");

            new ReleaseSafetyValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "Git.Metadata", BuildValidationSeverity.Error));
        }

        [Test]
        public void Integration_RemoteConfigEnvironmentMismatchInProduction_IsError()
        {
            _profile.Environment = DeploymentEnvironment.Production;
            _profile.DevelopmentBuild = false;
            BuildContext context = Resolve(out BuildValidationReport report);

            // No configuration asset assigned = runtime default (Development) environment.
            context.SceneScan.ScanText("Assets/Test.unity", SceneYaml(ScriptGuid<RemoteConfigBootstrapper>(), "  _remoteConfigConfiguration: {fileID: 0}"));

            new FrameworkIntegrationValidator().Validate(context, report);

            Assert.IsTrue(Has(report, "RemoteConfig.Environment", BuildValidationSeverity.Error));
        }

        [Test]
        public void Integration_EnvironmentMapping()
        {
            Assert.AreEqual(RemoteConfigEnvironment.Production, FrameworkIntegrationValidator.MapEnvironment(DeploymentEnvironment.Production));
            Assert.IsNull(FrameworkIntegrationValidator.MapEnvironment(DeploymentEnvironment.Unspecified));
            Assert.IsTrue(FrameworkIntegrationValidator.IsGooglePlayTestProductId("android.test.purchased"));
            Assert.IsFalse(FrameworkIntegrationValidator.IsGooglePlayTestProductId("com.game.coins_100"));
        }

        [TestCase("com.company.game", true)]
        [TestCase("com.company.game.dev", true)]
        [TestCase("game", false)]
        [TestCase("com..game", false)]
        [TestCase("com.1company.game", false)]
        [TestCase("com.company-name.game", false)]
        [TestCase("", false)]
        public void ApplicationIdentifierFormat(string id, bool valid)
        {
            Assert.AreEqual(valid, PlayerSettingsValidator.IsValidApplicationIdentifier(id));
        }

        [TestCase("com.DefaultCompany.MyGame", true)]
        [TestCase("com.example.game", true)]
        [TestCase("com.acme.racer", false)]
        public void Placeholder_Detection(string id, bool placeholder)
        {
            Assert.AreEqual(placeholder, PlayerSettingsValidator.ContainsPlaceholder(id));
        }

        [Test]
        public void Scanner_ResolvesSubclassTypeFieldsAndReferences()
        {
            var scanner = new SceneComponentScanner();
            scanner.ScanText("Assets/Test.unity", SceneYaml(ScriptGuid<RemoteConfigBootstrapper>(),
                "  _useMockProvider: 0\n  _remoteConfigConfiguration: {fileID: 11400000, guid: 0123456789abcdef0123456789abcdef, type: 2}"));

            ScannedComponent component = scanner.Components[0];
            Assert.IsTrue(component.Is<GameBootstrapper>(), "RemoteConfigBootstrapper derives from GameBootstrapper.");
            Assert.AreEqual("0", component.GetField("_useMockProvider"));
            Assert.AreEqual("0123456789abcdef0123456789abcdef", component.GetReferenceGuid("_remoteConfigConfiguration"));
            Assert.IsNull(component.GetField("_notAField"));
        }

        [Test]
        public void Scanner_UnknownScriptGuid_ReportedAsMissing()
        {
            var scanner = new SceneComponentScanner();
            scanner.ScanText("Assets/Test.unity", SceneYaml("ffffffffffffffffffffffffffffffff", string.Empty));

            Assert.AreEqual(1, scanner.MissingScripts.Count);
        }

        [Test]
        public void VersionHistory_FindsHighestPreviousBuildNumber()
        {
            Directory.CreateDirectory(_tempDirectory);
            File.WriteAllText(Path.Combine(_tempDirectory, "A.build.json"), JsonUtility.ToJson(new BuildMetadata { BuildNumber = 12 }));
            File.WriteAllText(Path.Combine(_tempDirectory, "B.build.json"), JsonUtility.ToJson(new BuildMetadata { BuildNumber = 30 }));
            File.WriteAllText(Path.Combine(_tempDirectory, "Corrupt.build.json"), "{ not json");

            Assert.AreEqual(12, VersionHistoryValidator.HighestPreviousBuildNumber(_tempDirectory, "B"));
            Assert.AreEqual(30, VersionHistoryValidator.HighestPreviousBuildNumber(_tempDirectory, null));
        }

        [Test]
        public void Pipeline_Validate_MissingScene_FailsWithoutWritingFiles()
        {
            _profile.Scenes = new[] { "Assets/DoesNotExist.unity" };

            BuildRunResult result = new FrameworkBuildPipeline().Validate(new BuildRequest(_profile));

            Assert.AreEqual(BuildRunStatus.ValidationFailed, result.Status);
            Assert.IsFalse(Directory.Exists(_tempDirectory), "Validate must have no side effects.");
        }

        [Test]
        public void Pipeline_AdditionalValidatorCanFailTheBuild()
        {
            _profile.Scenes = new[] { "Assets/GameFramework/Samples/Phase1Demo/Phase1Demo.unity" };
            var pipeline = new FrameworkBuildPipeline(new IBuildValidator[] { new AlwaysFails() });

            BuildRunResult result = pipeline.Validate(new BuildRequest(_profile));

            Assert.AreEqual(BuildRunStatus.ValidationFailed, result.Status);
            Assert.IsTrue(Has(result.Validation, "Project.Custom", BuildValidationSeverity.Error));
        }

        [Test]
        public void Pipeline_ThrowingValidator_BecomesErrorNotSkipped()
        {
            var pipeline = new FrameworkBuildPipeline(new IBuildValidator[] { new Throws() });

            BuildRunResult result = pipeline.Validate(new BuildRequest(_profile));

            Assert.IsTrue(Has(result.Validation, "Validator.Throws", BuildValidationSeverity.Error));
        }

        [Test]
        public void Pipeline_NullProfile_IsInvalidRequest()
        {
            Assert.AreEqual(BuildRunStatus.InvalidRequest, new FrameworkBuildPipeline().Validate(new BuildRequest(null)).Status);
        }

        private static string ScriptGuid<T>()
        {
            foreach (string guid in AssetDatabase.FindAssets(typeof(T).Name + " t:MonoScript"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == typeof(T))
                {
                    return guid;
                }
            }

            Assert.Fail($"No MonoScript for {typeof(T).Name}.");
            return null;
        }

        private static string SceneYaml(string scriptGuid, string fields) =>
            "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &200\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n" +
            $"  m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}\n  m_Name: \n" + fields + "\n";

        private sealed class AlwaysFails : IBuildValidator
        {
            public void Validate(BuildContext context, BuildValidationReport report) => report.Error("Project.Custom", "Game-specific rule failed.");
        }

        private sealed class Throws : IBuildValidator
        {
            public void Validate(BuildContext context, BuildValidationReport report) => throw new InvalidOperationException("boom");
        }
    }
}
