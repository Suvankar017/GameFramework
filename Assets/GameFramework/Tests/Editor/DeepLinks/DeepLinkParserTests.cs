using NUnit.Framework;

namespace GameFramework.DeepLinks.Tests
{
    public class DeepLinkParserTests
    {
        [Test]
        public void TryParse_CustomScheme_NormalizesHostIntoPath()
        {
            bool success = DeepLinkParser.TryParse("mygame://daily-reward?source=notification", out DeepLink link);

            Assert.IsTrue(success);
            Assert.AreEqual("mygame", link.Scheme);
            Assert.AreEqual("/daily-reward", link.Path);
            Assert.IsTrue(link.TryGetQueryParameter("source", out string source));
            Assert.AreEqual("notification", source);
        }

        [Test]
        public void TryParse_HttpsUrl_KeepsHostAndPathSeparate()
        {
            bool success = DeepLinkParser.TryParse("https://example.com/game/daily-reward?id=123", out DeepLink link);

            Assert.IsTrue(success);
            Assert.AreEqual("https", link.Scheme);
            Assert.AreEqual("example.com", link.Host);
            Assert.AreEqual("/game/daily-reward", link.Path);
            Assert.IsTrue(link.TryGetQueryParameter("id", out string id));
            Assert.AreEqual("123", id);
        }

        [Test]
        public void TryParse_MultipleQueryParameters_ParsesAll()
        {
            DeepLinkParser.TryParse("mygame://shop?item=sword&qty=2", out DeepLink link);

            Assert.AreEqual("sword", link.QueryParameters["item"]);
            Assert.AreEqual("2", link.QueryParameters["qty"]);
        }

        [Test]
        public void TryParse_EncodedQueryValue_Decodes()
        {
            DeepLinkParser.TryParse("mygame://search?q=hello%20world", out DeepLink link);

            Assert.AreEqual("hello world", link.QueryParameters["q"]);
        }

        [Test]
        public void TryParse_Fragment_IsCaptured()
        {
            DeepLinkParser.TryParse("https://example.com/page#section2", out DeepLink link);

            Assert.AreEqual("section2", link.Fragment);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not a uri at all")]
        [TestCase("://missing-scheme")]
        public void TryParse_MalformedOrEmpty_ReturnsFalse(string rawUri)
        {
            bool success = DeepLinkParser.TryParse(rawUri, out DeepLink link);

            Assert.IsFalse(success);
            Assert.AreEqual(default(DeepLink).Path, link.Path);
        }

        [Test]
        public void TryParse_RawUri_IsPreservedVerbatim()
        {
            const string raw = "mygame://daily-reward?source=notification";
            DeepLinkParser.TryParse(raw, out DeepLink link);

            Assert.AreEqual(raw, link.RawUri);
            Assert.AreEqual(raw, link.ToString());
        }
    }
}
