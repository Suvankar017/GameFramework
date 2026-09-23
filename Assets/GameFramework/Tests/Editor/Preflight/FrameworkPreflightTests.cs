using GameFramework.Editor.Security;
using NUnit.Framework;

namespace GameFramework.Editor.Tests
{
    /// <summary>Covers <see cref="FrameworkPreflight"/>'s pure detection helpers. Fake credential
    /// shapes are assembled by concatenation so this file never contains a literal that looks like a
    /// real secret to repository scanners.</summary>
    public class FrameworkPreflightTests
    {
        [Test]
        public void FindSecretPatterns_AwsKeyId_DetectedWithLineButNotValue()
        {
            string text = "line one\nkey = " + "AKIA" + "ABCDEFGHIJKLMNOP" + "\n";

            var found = FrameworkPreflight.FindSecretPatterns(text);

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual(2, found[0].Line);
            StringAssert.DoesNotContain("ABCDEFGHIJKLMNOP", found[0].PatternName);
        }

        [Test]
        public void FindSecretPatterns_PrivateKeyBlock_Detected()
        {
            string text = "-----BEGIN " + "PRIVATE KEY-----\nMIIE...";

            Assert.AreEqual(1, FrameworkPreflight.FindSecretPatterns(text).Count);
        }

        [Test]
        public void FindSecretPatterns_HardCodedClientSecretAssignment_Detected()
        {
            string text = "private const string client_secret = \"" + "abcd1234efgh5678" + "\";";

            Assert.AreEqual(1, FrameworkPreflight.FindSecretPatterns(text).Count);
        }

        [Test]
        public void FindSecretPatterns_OrdinaryCode_NoFindings()
        {
            const string text = "private const string SaveKey = \"GameFramework.Monetization.Purchases\";\npublic string Token => _token;";

            Assert.AreEqual(0, FrameworkPreflight.FindSecretPatterns(text).Count);
        }

        [Test]
        public void FindEnabledMockToggles_ReadsSerializedYaml()
        {
            const string yaml = "  _adConfiguration: {fileID: 0}\n  _useMockProviders: 1\n  _useMockAnalyticsProvider: 0\n";

            var enabled = FrameworkPreflight.FindEnabledMockToggles(yaml);

            CollectionAssert.AreEqual(new[] { "_useMockProviders" }, enabled);
        }

        [Test]
        public void FindDuplicates_ReportsEachDuplicateOnceAndIgnoresEmpty()
        {
            var duplicates = FrameworkPreflight.FindDuplicates(new[] { "remove_ads", "coins", "remove_ads", "remove_ads", "", null, "" });

            CollectionAssert.AreEqual(new[] { "remove_ads" }, duplicates);
        }
    }
}
