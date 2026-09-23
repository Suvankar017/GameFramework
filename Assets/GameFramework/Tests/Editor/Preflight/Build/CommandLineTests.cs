using GameFramework.Editor.Build;
using GameFramework.Runtime.Security;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Tests.Build
{
    public class CommandLineTests
    {
        private FrameworkBuildProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<FrameworkBuildProfile>();
            _profile.ProfileId = "AndroidStaging";
            _profile.Target = BuildTarget.Android;
            _profile.Environment = DeploymentEnvironment.Staging;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_profile);

        [Test]
        public void Parse_AllOptions_AmongUnityArguments()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[]
            {
                "Unity.exe", "-batchmode", "-quit", "-projectPath", "C:/p", "-executeMethod", "GameFramework.Editor.Build.CommandLineBuild.Build",
                "-profile", "AndroidStaging", "-environment", "Staging", "-buildTarget", "Android",
                "-outputPath", "out", "-version", "1.4.0", "-buildNumber", "10400", "-validateOnly", "-overwrite"
            });

            Assert.IsTrue(args.IsValid, string.Join("; ", args.Errors));
            Assert.AreEqual("AndroidStaging", args.Profile);
            Assert.AreEqual("Staging", args.Environment);
            Assert.AreEqual("Android", args.BuildTarget);
            Assert.AreEqual("out", args.OutputPath);
            Assert.AreEqual("1.4.0", args.Version);
            Assert.AreEqual(10400, args.BuildNumber);
            Assert.IsTrue(args.ValidateOnly);
            Assert.IsTrue(args.Overwrite);
        }

        [Test]
        public void Parse_OptionNamesAreCaseInsensitive()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-PROFILE", "X", "-BuildNumber", "3" });

            Assert.IsTrue(args.IsValid);
            Assert.AreEqual(3, args.BuildNumber);
        }

        [Test]
        public void Parse_MissingProfile_IsError()
        {
            Assert.IsFalse(CommandLineArguments.Parse(new[] { "Unity.exe", "-batchmode" }).IsValid);
        }

        [Test]
        public void Parse_MissingValue_IsError()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "X", "-version", "-validateOnly" });

            Assert.IsFalse(args.IsValid);
            StringAssert.Contains("-version requires a value", args.Errors[0]);
        }

        [Test]
        public void Parse_ValueAtEnd_IsError()
        {
            Assert.IsFalse(CommandLineArguments.Parse(new[] { "-profile" }).IsValid);
        }

        [Test]
        public void Parse_DuplicateOption_IsError()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "A", "-profile", "B" });

            Assert.IsFalse(args.IsValid);
            StringAssert.Contains("more than once", args.Errors[0]);
        }

        [Test]
        public void Parse_NonIntegerBuildNumber_IsError()
        {
            Assert.IsFalse(CommandLineArguments.Parse(new[] { "-profile", "A", "-buildNumber", "12a" }).IsValid);
        }

        [Test]
        public void Parse_NegativeBuildNumber_IsParsedSoValidationCanRejectIt()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "A", "-buildNumber", "-3" });

            Assert.IsTrue(args.IsValid);
            Assert.AreEqual(-3, args.BuildNumber);
        }

        [Test]
        public void CreateRequest_MatchingAssertions_CarriesOverrides()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "AndroidStaging", "-environment", "staging", "-buildTarget", "Android", "-version", "2.0", "-buildNumber", "20000" });

            Assert.IsTrue(CommandLineBuild.TryCreateRequest(args, _profile, out BuildRequest request, out string error), error);
            Assert.AreEqual("2.0", request.VersionOverride);
            Assert.AreEqual(20000, request.BuildNumberOverride);
            Assert.IsFalse(request.ValidateOnly);
        }

        [Test]
        public void CreateRequest_EnvironmentMismatch_IsRejected()
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "AndroidStaging", "-environment", "Production" });

            Assert.IsFalse(CommandLineBuild.TryCreateRequest(args, _profile, out _, out string error));
            StringAssert.Contains("does not match", error);
        }

        [TestCase("Qa")]
        [TestCase("Unspecified")]
        public void CreateRequest_InvalidEnvironment_IsRejected(string environment)
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "AndroidStaging", "-environment", environment });

            Assert.IsFalse(CommandLineBuild.TryCreateRequest(args, _profile, out _, out _));
        }

        [TestCase("iOS")]
        [TestCase("NotATarget")]
        [TestCase("13")]
        public void CreateRequest_WrongOrInvalidTarget_IsRejected(string target)
        {
            CommandLineArguments args = CommandLineArguments.Parse(new[] { "-profile", "AndroidStaging", "-buildTarget", target });

            Assert.IsFalse(CommandLineBuild.TryCreateRequest(args, _profile, out _, out _));
        }

        [TestCase("Win64", BuildTarget.StandaloneWindows64)]
        [TestCase("OSXUniversal", BuildTarget.StandaloneOSX)]
        [TestCase("Linux64", BuildTarget.StandaloneLinux64)]
        [TestCase("android", BuildTarget.Android)]
        public void TryParseBuildTarget_UnityAliases(string value, BuildTarget expected)
        {
            Assert.IsTrue(CommandLineBuild.TryParseBuildTarget(value, out BuildTarget target));
            Assert.AreEqual(expected, target);
        }

        [TestCase(BuildRunStatus.Succeeded, CommandLineBuild.ExitCode.Success)]
        [TestCase(BuildRunStatus.ValidationPassed, CommandLineBuild.ExitCode.Success)]
        [TestCase(BuildRunStatus.InvalidRequest, CommandLineBuild.ExitCode.InvalidArguments)]
        [TestCase(BuildRunStatus.ValidationFailed, CommandLineBuild.ExitCode.ValidationFailed)]
        [TestCase(BuildRunStatus.BuildFailed, CommandLineBuild.ExitCode.BuildFailed)]
        [TestCase(BuildRunStatus.PostBuildValidationFailed, CommandLineBuild.ExitCode.PostBuildValidationFailed)]
        public void ExitCodes_AreNonZeroForEveryFailure(BuildRunStatus status, CommandLineBuild.ExitCode expected)
        {
            Assert.AreEqual(expected, CommandLineBuild.ToExitCode(new BuildRunResult { Status = status }));
        }
    }
}
