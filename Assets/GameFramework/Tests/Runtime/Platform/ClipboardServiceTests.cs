using NUnit.Framework;

namespace GameFramework.Platform.Tests
{
    public class ClipboardServiceTests
    {
        private ClipboardService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new ClipboardService();
        }

        [Test]
        public void IsSupported_IsAlwaysTrue()
        {
            Assert.IsTrue(_service.IsSupported);
        }

        [Test]
        public void SetTextThenGetText_RoundTrips()
        {
            _service.SetText("hello platform");

            Assert.AreEqual("hello platform", _service.GetText());
            Assert.IsTrue(_service.HasText());
        }

        [Test]
        public void SetText_Null_ClearsClipboard()
        {
            _service.SetText("something");
            _service.SetText(null);

            Assert.IsFalse(_service.HasText());
        }
    }
}
