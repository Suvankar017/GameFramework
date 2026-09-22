using System;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Analytics.Tests
{
    public class DiagnosticsServiceTests
    {
        private FakeCrashReportingProvider _provider;
        private DiagnosticsService _diagnostics;

        private void SetUp(bool enabled = true, int breadcrumbCapacity = 50)
        {
            ServiceRegistry registry = TestRegistryFactory.Build(out EventService _, out PersistenceService _);

            _provider = new FakeCrashReportingProvider();
            var config = TestDefinitions.DiagnosticsConfig(enabled: enabled, breadcrumbCapacity: breadcrumbCapacity, captureUnhandledExceptions: false);
            _diagnostics = new DiagnosticsService(config, _provider);
            _diagnostics.Initialize(registry);
        }

        [TearDown]
        public void TearDown() => _diagnostics?.Shutdown();

        [Test]
        public void RecordException_Enabled_ForwardsToProvider()
        {
            SetUp(enabled: true);

            _diagnostics.RecordException(new InvalidOperationException("test"), ErrorCategory.Gameplay);

            Assert.AreEqual(1, _provider.ReceivedReports.Count);
            Assert.AreEqual(ErrorCategory.Gameplay, _provider.ReceivedReports[0].Category);
        }

        [Test]
        public void RecordException_Disabled_DoesNotReachProvider()
        {
            SetUp(enabled: false);

            _diagnostics.RecordException(new InvalidOperationException("test"));

            Assert.AreEqual(0, _provider.ReceivedReports.Count);
        }

        [Test]
        public void RecordException_NullException_IsNoOp()
        {
            SetUp();

            Assert.DoesNotThrow(() => _diagnostics.RecordException(null));
            Assert.AreEqual(0, _provider.ReceivedReports.Count);
        }

        [Test]
        public void RecordError_Enabled_ForwardsToProvider()
        {
            SetUp();

            _diagnostics.RecordError("something went wrong", ErrorCategory.Network);

            Assert.AreEqual(1, _provider.ReceivedReports.Count);
            Assert.AreEqual("something went wrong", _provider.ReceivedReports[0].Message);
            Assert.IsNull(_provider.ReceivedReports[0].Exception);
        }

        [Test]
        public void Breadcrumbs_AreBounded()
        {
            SetUp(breadcrumbCapacity: 3);

            for (int i = 0; i < 5; i++)
            {
                _diagnostics.AddBreadcrumb("Test", $"crumb-{i}");
            }

            var breadcrumbs = _diagnostics.GetBreadcrumbs();
            Assert.AreEqual(3, breadcrumbs.Count);
            Assert.AreEqual("crumb-2", breadcrumbs[0].Message);
            Assert.AreEqual("crumb-4", breadcrumbs[2].Message);
        }

        [Test]
        public void RecordException_IncludesContextTagsAndBreadcrumbs()
        {
            SetUp();
            _diagnostics.SetContext("level", "1-1");
            _diagnostics.SetTag("build_type", "development");
            _diagnostics.AddBreadcrumb("Gameplay", "player_spawned");

            _diagnostics.RecordException(new InvalidOperationException("test"));

            DiagnosticReport report = _provider.ReceivedReports[0];
            Assert.AreEqual("1-1", report.Context["level"]);
            Assert.AreEqual("development", report.Tags["build_type"]);
            Assert.AreEqual(1, report.Breadcrumbs.Count);
        }

        [Test]
        public void RecordException_ProviderThrows_DoesNotThrow_AndDoesNotRecurse()
        {
            SetUp();
            _provider.ThrowOnReport = true;

            Assert.DoesNotThrow(() => _diagnostics.RecordException(new InvalidOperationException("test")));

            // The provider failure itself must never be re-reported through RecordException/RecordError.
            Assert.AreEqual(1, _provider.ReportCallCount);
        }

        [Test]
        public void SetContext_EmptyKey_IsIgnored()
        {
            SetUp();

            Assert.DoesNotThrow(() => _diagnostics.SetContext(string.Empty, "value"));
            Assert.DoesNotThrow(() => _diagnostics.SetContext(null, "value"));
        }
    }
}
