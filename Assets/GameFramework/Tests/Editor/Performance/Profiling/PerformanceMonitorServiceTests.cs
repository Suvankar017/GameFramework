using GameFramework.Performance.Profiling;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using NUnit.Framework;

namespace GameFramework.Performance.Tests.Profiling
{
    public class PerformanceMonitorServiceTests
    {
        private FakeTimeService _time;
        private PerformanceMonitorService _monitor;

        [SetUp]
        public void SetUp()
        {
            PerformanceSettings.Mode = ProfilingMode.Development;

            var registry = new ServiceRegistry();
            _time = new FakeTimeService { UnscaledDeltaTime = 0.016f, Realtime = 0f };
            registry.Register<ITimeService>(_time);
            registry.MarkInitialized(typeof(ITimeService));

            _monitor = new PerformanceMonitorService();
            _monitor.Initialize(registry);
        }

        [TearDown]
        public void TearDown()
        {
            _monitor.Shutdown();
        }

        [Test]
        public void Tick_SamplesLastFrameTimeFromUnscaledDeltaTime()
        {
            _time.UnscaledDeltaTime = 0.02f;

            _monitor.Tick();

            Assert.AreEqual(20f, _monitor.GetFrameStats().LastFrameMilliseconds, 0.01f);
        }

        [Test]
        public void Tick_Disabled_DoesNotSample()
        {
            PerformanceSettings.Mode = ProfilingMode.Disabled;

            _monitor.Tick();

            Assert.AreEqual(0, _monitor.GetFrameStats().SampleCount);
        }

        [Test]
        public void GetFrameStats_WorstFrame_TracksHighestSampleSeen()
        {
            _time.UnscaledDeltaTime = 0.01f;
            _monitor.Tick();
            _time.UnscaledDeltaTime = 0.05f;
            _monitor.Tick();
            _time.UnscaledDeltaTime = 0.01f;
            _monitor.Tick();

            Assert.AreEqual(50f, _monitor.GetFrameStats().WorstFrameMilliseconds, 0.01f);
        }

        [Test]
        public void ResetFrameStats_ClearsWorstAndSpikeCount()
        {
            _monitor.SpikeThresholdMilliseconds = 5f;
            _time.UnscaledDeltaTime = 0.05f;
            _monitor.Tick();
            Assert.Greater(_monitor.GetFrameStats().SpikeCount, 0);

            _monitor.ResetFrameStats();

            FrameTimeStats stats = _monitor.GetFrameStats();
            Assert.AreEqual(0, stats.SpikeCount);
            Assert.AreEqual(0, stats.WorstFrameMilliseconds);
            Assert.AreEqual(0, stats.SampleCount);
        }

        [Test]
        public void ReportSample_UnderBudget_DoesNotThrow()
        {
            _monitor.RegisterBudget("GameplayTick", 8f);

            Assert.DoesNotThrow(() => _monitor.ReportSample("GameplayTick", 3f));
        }

        [Test]
        public void ReportSample_UnregisteredName_IsANoOp()
        {
            Assert.DoesNotThrow(() => _monitor.ReportSample("NeverRegistered", 999f));
        }

        [Test]
        public void ReportSample_OverBudget_DoesNotThrow()
        {
            _monitor.RegisterBudget("GameplayTick", 8f);

            Assert.DoesNotThrow(() => _monitor.ReportSample("GameplayTick", 15f));
        }

        [Test]
        public void FrameBudgetMilliseconds_MatchesTargetFrameRate()
        {
            _monitor.TargetFrameRate = 30;
            Assert.AreEqual(1000f / 30f, _monitor.FrameBudgetMilliseconds, 0.001f);

            _monitor.TargetFrameRate = 60;
            Assert.AreEqual(1000f / 60f, _monitor.FrameBudgetMilliseconds, 0.001f);
        }
    }
}
