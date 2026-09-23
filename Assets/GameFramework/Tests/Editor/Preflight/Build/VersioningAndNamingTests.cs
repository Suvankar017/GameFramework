using GameFramework.Editor.Build;
using GameFramework.Runtime.Security;
using NUnit.Framework;
using UnityEditor;

namespace GameFramework.Editor.Tests.Build
{
    public class VersioningAndNamingTests
    {
        [TestCase("1", 1, 0, 0)]
        [TestCase("1.4", 1, 4, 0)]
        [TestCase("1.4.0", 1, 4, 0)]
        [TestCase("10.20.30", 10, 20, 30)]
        public void SemanticVersion_ValidForms_Parse(string text, int major, int minor, int patch)
        {
            Assert.IsTrue(SemanticVersion.TryParse(text, out SemanticVersion version, out string error), error);
            Assert.AreEqual(major, version.Major);
            Assert.AreEqual(minor, version.Minor);
            Assert.AreEqual(patch, version.Patch);
            Assert.AreEqual(text, version.ToString(), "Formatting must round-trip the source component count.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" 1.0")]
        [TestCase("1.0.0.0")]
        [TestCase("1.0-beta")]
        [TestCase("1..0")]
        [TestCase("v1.0")]
        [TestCase("1.-1")]
        [TestCase("100000.0.0")]
        public void SemanticVersion_InvalidForms_Rejected(string text)
        {
            Assert.IsFalse(SemanticVersion.TryParse(text, out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void SemanticVersion_Ordering()
        {
            SemanticVersion.TryParse("1.4.0", out SemanticVersion a, out _);
            SemanticVersion.TryParse("1.10.0", out SemanticVersion b, out _);

            Assert.Less(a.CompareTo(b), 0);
        }

        [TestCase("1.4.0", 10400)]
        [TestCase("2.0.1", 20001)]
        [TestCase("0.0.1", 1)]
        public void Encode_MapsVersionToBuildNumber(string text, int expected)
        {
            SemanticVersion.TryParse(text, out SemanticVersion version, out _);

            Assert.IsTrue(BuildNumbers.TryEncode(version, out int number, out _));
            Assert.AreEqual(expected, number);
        }

        [TestCase("1.100.0")]
        [TestCase("1.0.100")]
        [TestCase("0.0.0")]
        public void Encode_AmbiguousOrZero_Rejected(string text)
        {
            SemanticVersion.TryParse(text, out SemanticVersion version, out _);

            Assert.IsFalse(BuildNumbers.TryEncode(version, out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Resolve_ExplicitOverrideWinsOverScheme()
        {
            SemanticVersion.TryParse("1.4.0", out SemanticVersion version, out _);

            int number = BuildNumbers.Resolve(BuildNumberScheme.EncodedFromVersion, version, 3, 42, out string error);

            Assert.IsNull(error);
            Assert.AreEqual(42, number);
        }

        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(2100000001)]
        public void Resolve_InvalidBuildNumber_ReportsError(int value)
        {
            SemanticVersion.TryParse("1.0", out SemanticVersion version, out _);

            BuildNumbers.Resolve(BuildNumberScheme.FromPlayerSettings, version, value, null, out string error);

            Assert.IsNotNull(error);
        }

        [Test]
        public void BuildNumbers_TryParse_RejectsNonIntegerIosBuildNumber()
        {
            Assert.IsTrue(BuildNumbers.TryParse("104", out int value));
            Assert.AreEqual(104, value);
            Assert.IsFalse(BuildNumbers.TryParse("1.0.4", out _));
            Assert.IsFalse(BuildNumbers.TryParse("", out _));
        }

        [Test]
        public void ArtifactName_IsDeterministicAndSanitized()
        {
            SemanticVersion.TryParse("1.4.0", out SemanticVersion version, out _);

            string first = ArtifactNaming.BaseName("My Game: Deluxe!", BuildTarget.Android, DeploymentEnvironment.Production, version, 10400);
            string second = ArtifactNaming.BaseName("My Game: Deluxe!", BuildTarget.Android, DeploymentEnvironment.Production, version, 10400);

            Assert.AreEqual("My-Game--Deluxe_Android_Production_1.4.0_10400", first);
            Assert.AreEqual(first, second);
        }

        [TestCase("")]
        [TestCase("!!!")]
        [TestCase(null)]
        public void ArtifactName_SlugWithNothingUsable_IsEmpty(string slug)
        {
            SemanticVersion.TryParse("1.0", out SemanticVersion version, out _);

            Assert.AreEqual(string.Empty, ArtifactNaming.BaseName(slug, BuildTarget.iOS, DeploymentEnvironment.Staging, version, 1));
        }

        [Test]
        public void LocationPath_PerTargetShape()
        {
            StringAssert.EndsWith("Game.aab", ArtifactNaming.LocationPath("out", "Game", BuildTarget.Android, true).Replace('\\', '/'));
            StringAssert.EndsWith("Game.apk", ArtifactNaming.LocationPath("out", "Game", BuildTarget.Android, false).Replace('\\', '/'));
            StringAssert.EndsWith("out/Game", ArtifactNaming.LocationPath("out", "Game", BuildTarget.iOS, false).Replace('\\', '/'));
            StringAssert.EndsWith("out/Game/Game.exe", ArtifactNaming.LocationPath("out", "Game", BuildTarget.StandaloneWindows64, false).Replace('\\', '/'));
        }

        [Test]
        public void OutputDirectory_SeparatesPlatformAndEnvironment()
        {
            string path = ArtifactNaming.OutputDirectory("Builds", BuildTarget.Android, DeploymentEnvironment.Staging).Replace('\\', '/');

            Assert.AreEqual("Builds/Android/Staging", path);
        }

        [Test]
        public void EnvironmentDefine_OnePerEnvironment()
        {
            Assert.AreEqual("GAMEFRAMEWORK_ENV_PRODUCTION", BuildDefines.EnvironmentDefine(DeploymentEnvironment.Production));
            Assert.AreEqual("GAMEFRAMEWORK_ENV_STAGING", BuildDefines.EnvironmentDefine(DeploymentEnvironment.Staging));
            Assert.IsNull(BuildDefines.EnvironmentDefine(DeploymentEnvironment.Unspecified));
        }

        [Test]
        public void ForBuild_AddsEnvironmentFirstAndDeduplicates()
        {
            var defines = BuildDefines.ForBuild(DeploymentEnvironment.Staging, new[] { " MY_FEATURE ", "MY_FEATURE", "", null, "GAMEFRAMEWORK_ENV_STAGING" });

            CollectionAssert.AreEqual(new[] { "GAMEFRAMEWORK_ENV_STAGING", "MY_FEATURE" }, defines);
        }

        [TestCase("DEBUG", true)]
        [TestCase("ENABLE_CHEATS", true)]
        [TestCase("USE_MOCK_IAP", true)]
        [TestCase("GODMODE_CHEAT", true)]
        [TestCase("GAME_ANALYTICS", false)]
        [TestCase("CUSTOM_FORBIDDEN", true)]
        public void ForbiddenInProduction(string symbol, bool forbidden)
        {
            Assert.AreEqual(forbidden, BuildDefines.IsForbiddenInProduction(symbol, new[] { "CUSTOM_FORBIDDEN" }));
        }

        [TestCase("VALID_1", true)]
        [TestCase("_underscore", true)]
        [TestCase("1STARTS_WITH_DIGIT", false)]
        [TestCase("HAS SPACE", false)]
        [TestCase("HAS-DASH", false)]
        public void IsValidSymbol(string symbol, bool valid)
        {
            Assert.AreEqual(valid, BuildDefines.IsValidSymbol(symbol));
        }
    }
}
