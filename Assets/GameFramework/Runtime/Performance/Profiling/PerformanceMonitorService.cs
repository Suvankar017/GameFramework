using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Default <see cref="IPerformanceMonitorService"/>. Samples real (unscaled) frame time once
    /// per frame via <see cref="Runtime.Services.IUpdatableService.Tick"/> - the same mechanism
    /// every other Phase 2+ per-frame service uses, driven by <c>GameBootstrapper.Update()</c>.
    ///
    /// <b>What this can tell you.</b> Frame time here is wall-clock CPU frame duration
    /// (<see cref="ITimeService.UnscaledDeltaTime"/>) as observed between two <see cref="Tick"/>
    /// calls - a real, measured value. It cannot isolate GPU time, cannot attribute a spike to a
    /// specific system, and is not a substitute for the Unity Profiler or a platform's native
    /// profiling tools. Treat <see cref="GetFrameStats"/> as "is something wrong right now", and
    /// use <see cref="ProfileScope"/> plus the Unity Profiler to find out what.
    ///
    /// <b>Target frame rate is a budget, not a guarantee.</b> <see cref="TargetFrameRate"/> only
    /// changes what counts as "over budget" for spike detection and the default
    /// <see cref="SpikeThresholdMilliseconds"/> - actual achievable frame rate depends on the
    /// device, resolution, scene content, and thermal state, none of which this service controls.
    /// </summary>
    public sealed class PerformanceMonitorService : IPerformanceMonitorService, IUpdatableService
    {
        private const string LogCategory = "Performance";
        private const int RollingSampleCapacity = 120;

        private readonly float[] _samples = new float[RollingSampleCapacity];
        private readonly Dictionary<string, PerformanceBudget> _budgets = new Dictionary<string, PerformanceBudget>();
        private readonly Dictionary<string, float> _lastBudgetWarningTime = new Dictionary<string, float>();

        private ITimeService _time;
        private ILoggingService _logging;

        private int _sampleCount;
        private int _sampleWriteIndex;
        private float _sampleSum;
        private float _lastFrameMs;
        private float _worstFrameMs;
        private int _spikeCount;
        private float _lastSpikeWarningTime = float.NegativeInfinity;

        public int TargetFrameRate { get; set; } = 30;

        public float FrameBudgetMilliseconds => 1000f / TargetFrameRate;

        public float SpikeThresholdMilliseconds { get; set; } = -1f;

        public float SpikeLogIntervalSeconds { get; set; } = 2f;

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();
            registry.TryGet(out _logging);
        }

        public void Shutdown()
        {
            _budgets.Clear();
            _lastBudgetWarningTime.Clear();
        }

        public void Tick()
        {
            if (!PerformanceSettings.IsProfilingEnabled)
            {
                return;
            }

            using (new ProfileScope(ProfilingCategory.Framework))
            {
                float frameMs = _time.UnscaledDeltaTime * 1000f;
                _lastFrameMs = frameMs;

                if (_sampleCount < RollingSampleCapacity)
                {
                    _sampleCount++;
                }
                else
                {
                    _sampleSum -= _samples[_sampleWriteIndex];
                }

                _samples[_sampleWriteIndex] = frameMs;
                _sampleSum += frameMs;
                _sampleWriteIndex = (_sampleWriteIndex + 1) % RollingSampleCapacity;

                if (frameMs > _worstFrameMs)
                {
                    _worstFrameMs = frameMs;
                }

                float threshold = SpikeThresholdMilliseconds > 0f ? SpikeThresholdMilliseconds : FrameBudgetMilliseconds;
                if (frameMs > threshold)
                {
                    _spikeCount++;
                    LogRateLimited(ref _lastSpikeWarningTime, SpikeLogIntervalSeconds,
                        $"Frame spike: {frameMs:F2}ms (threshold {threshold:F2}ms).");
                }
            }
        }

        public FrameTimeStats GetFrameStats()
        {
            float average = _sampleCount > 0 ? _sampleSum / _sampleCount : 0f;
            return new FrameTimeStats(_lastFrameMs, average, _worstFrameMs, _spikeCount, _sampleCount);
        }

        public void ResetFrameStats()
        {
            _sampleCount = 0;
            _sampleWriteIndex = 0;
            _sampleSum = 0f;
            _worstFrameMs = 0f;
            _spikeCount = 0;
        }

        public void RegisterBudget(string name, float budgetMilliseconds)
        {
            Guard.NotNullOrEmpty(name, nameof(name));
            _budgets[name] = new PerformanceBudget(name, budgetMilliseconds);
        }

        public void ReportSample(string name, float elapsedMilliseconds)
        {
            if (!PerformanceSettings.IsProfilingEnabled || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!_budgets.TryGetValue(name, out PerformanceBudget budget) || elapsedMilliseconds <= budget.BudgetMilliseconds)
            {
                return;
            }

            float lastWarning = _lastBudgetWarningTime.TryGetValue(name, out float t) ? t : float.NegativeInfinity;
            LogRateLimited(ref lastWarning, SpikeLogIntervalSeconds,
                $"'{name}' exceeded its budget: {elapsedMilliseconds:F2}ms > {budget.BudgetMilliseconds:F2}ms.");
            _lastBudgetWarningTime[name] = lastWarning;
        }

        private void LogRateLimited(ref float lastLogTime, float interval, string message)
        {
            float now = _time.Realtime;
            if (now - lastLogTime < interval)
            {
                return;
            }

            lastLogTime = now;
            if (_logging != null)
            {
                _logging.Log(LogLevel.Warning, LogCategory, message);
            }
            else
            {
                Log.Warning(LogCategory, message);
            }
        }
    }
}
