using NUnit.Framework;

namespace GameFramework.Platform.Tests
{
    public class PlatformUrlServiceTests
    {
        [TestCase("https://example.com")]
        [TestCase("http://example.com/page?query=1")]
        [TestCase("mailto:test@example.com")]
        public void IsValidUrl_WellFormedAbsoluteUri_ReturnsTrue(string url)
        {
            Assert.IsTrue(PlatformUrlService.IsValidUrl(url));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not a url")]
        [TestCase("relative/path")]
        public void IsValidUrl_InvalidOrRelative_ReturnsFalse(string url)
        {
            Assert.IsFalse(PlatformUrlService.IsValidUrl(url));
        }

        [Test]
        public void OpenUrl_InvalidUrl_ReturnsFalseWithoutThrowing()
        {
            var service = new PlatformUrlService();

            Assert.IsFalse(service.OpenUrl("not a url"));
        }
    }
}
