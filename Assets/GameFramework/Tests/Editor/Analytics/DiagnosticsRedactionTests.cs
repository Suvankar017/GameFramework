using GameFramework.Analytics.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Analytics.Tests
{
    /// <summary>Phase 19: diagnostic context/tags/breadcrumbs/error messages are redacted and bounded
    /// before they can reach a crash-reporting provider.</summary>
    public class DiagnosticsRedactionTests
    {
        private FakeCrashReportingProvider _provider;
        private DiagnosticsService _diagnostics;

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out EventService _, out PersistenceService _);
            _provider = new FakeCrashReportingProvider();
            var config = TestDefinitions.DiagnosticsConfig(enabled: true, breadcrumbCapacity: 50, captureUnhandledExceptions: false);
            _diagnostics = new DiagnosticsService(config, _provider);
            _diagnostics.Initialize(registry);
        }

        [TearDown]
        public void TearDown() => _diagnostics.Shutdown();

        [Test]
        public void SetContext_SensitiveKey_ValueMasked()
        {
            _diagnostics.SetContext("auth_token", "eyJhbGciOiJIUzI1NiJ9.secret");
            _diagnostics.RecordError("boom");

            Assert.AreEqual("***", _provider.ReceivedReports[0].Context["auth_token"]);
        }

        [Test]
        public void RecordError_MessageWithCredential_RedactedBeforeReport()
        {
            _diagnostics.RecordError("Login failed for url https://api.example.com/login?password=hunter2&user=5");

            string message = _provider.ReceivedReports[0].Message;
            StringAssert.DoesNotContain("hunter2", message);
            StringAssert.Contains("user=5", message);
        }

        [Test]
        public void AddBreadcrumb_SensitivePairAndOversizedMessage_RedactedAndTruncated()
        {
            _diagnostics.AddBreadcrumb("Net", "receipt=ABCDEF123 " + new string('x', DiagnosticsService.MaxValueLength * 2));

            Breadcrumb crumb = _diagnostics.GetBreadcrumbs()[0];
            StringAssert.DoesNotContain("ABCDEF123", crumb.Message);
            Assert.LessOrEqual(crumb.Message.Length, DiagnosticsService.MaxValueLength);
        }

        [Test]
        public void SetTag_BeyondEntryLimit_IsDropped()
        {
            for (int i = 0; i < DiagnosticsService.MaxContextEntries + 10; i++)
            {
                _diagnostics.SetTag("tag" + i, "v");
            }

            _diagnostics.RecordError("boom");

            Assert.AreEqual(DiagnosticsService.MaxContextEntries, _provider.ReceivedReports[0].Tags.Count);
        }

        [Test]
        public void SetContext_OrdinaryValue_Unchanged()
        {
            _diagnostics.SetContext("level", "1-1");
            _diagnostics.RecordError("boom");

            Assert.AreEqual("1-1", _provider.ReceivedReports[0].Context["level"]);
        }
    }
}
