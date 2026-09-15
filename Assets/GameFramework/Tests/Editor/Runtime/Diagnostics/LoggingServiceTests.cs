using GameFramework.Runtime.Diagnostics;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Diagnostics
{
    public class LoggingServiceTests
    {
        private LoggingService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new LoggingService();
        }

        [Test]
        public void IsEnabled_LevelAtOrAboveMinimum_ReturnsTrue()
        {
            _service.MinimumLevel = LogLevel.Warning;

            Assert.IsTrue(_service.IsEnabled(LogLevel.Warning, "Test"));
            Assert.IsTrue(_service.IsEnabled(LogLevel.Error, "Test"));
        }

        [Test]
        public void IsEnabled_LevelBelowMinimum_ReturnsFalse()
        {
            _service.MinimumLevel = LogLevel.Warning;

            Assert.IsFalse(_service.IsEnabled(LogLevel.Info, "Test"));
            Assert.IsFalse(_service.IsEnabled(LogLevel.Debug, "Test"));
        }

        [Test]
        public void IsEnabled_DisabledCategory_ReturnsFalseRegardlessOfLevel()
        {
            _service.MinimumLevel = LogLevel.Trace;
            _service.SetCategoryEnabled("Audio", false);

            Assert.IsFalse(_service.IsEnabled(LogLevel.Fatal, "Audio"));
        }

        [Test]
        public void IsCategoryEnabled_UnknownCategory_DefaultsToTrue()
        {
            Assert.IsTrue(_service.IsCategoryEnabled("NeverConfigured"));
        }

        [Test]
        public void SetCategoryEnabled_ReenablingADisabledCategory_RestoresIt()
        {
            _service.SetCategoryEnabled("Audio", false);
            Assert.IsFalse(_service.IsCategoryEnabled("Audio"));

            _service.SetCategoryEnabled("Audio", true);

            Assert.IsTrue(_service.IsCategoryEnabled("Audio"));
        }

        [Test]
        public void Initialize_BindsToLogFacade_SoLogMethodsStopBeingNoOps()
        {
            Assert.IsFalse(Log.IsEnabled(LogLevel.Info, "Test"));

            _service.Initialize(null);

            Assert.IsTrue(Log.IsEnabled(LogLevel.Info, "Test"));

            _service.Shutdown();
        }

        [Test]
        public void Shutdown_UnbindsFromLogFacade()
        {
            _service.Initialize(null);
            Assert.IsTrue(Log.IsEnabled(LogLevel.Info, "Test"));

            _service.Shutdown();

            Assert.IsFalse(Log.IsEnabled(LogLevel.Info, "Test"));
        }

        [Test]
        public void Shutdown_DoesNotUnbindADifferentServiceThatSubsequentlyBound()
        {
            var first = new LoggingService();
            var second = new LoggingService();

            first.Initialize(null);
            second.Initialize(null);

            // first.Shutdown() must not clear the binding second just established.
            first.Shutdown();

            Assert.IsTrue(Log.IsEnabled(LogLevel.Info, "Test"));

            second.Shutdown();
        }
    }
}
