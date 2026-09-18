using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Presentation.Tests
{
    /// <summary>See <c>GameFramework.GameFlow.Tests.FakeTimeService</c>'s remarks - same pattern,
    /// duplicated per test assembly. <see cref="SetTimeScale"/>/<see cref="ResetTimeScale"/> record
    /// every call so tests can assert exactly what <see cref="PresentationService"/> requested,
    /// without needing the real engine's actual timeScale side effect.</summary>
    internal sealed class FakeTimeService : ITimeService
    {
        private int _pauseCount;

        public float ScaledDeltaTime { get; set; }
        public float UnscaledDeltaTime { get; set; }
        public float FixedDeltaTime { get; set; }
        public float UnscaledFixedDeltaTime { get; set; }
        public float ScaledTime { get; set; }
        public float UnscaledTime { get; set; }
        public float Realtime { get; set; }
        public float TimeScale { get; private set; } = 1f;
        public bool IsPaused => _pauseCount > 0;

        public int SetTimeScaleCallCount { get; private set; }
        public int ResetTimeScaleCallCount { get; private set; }

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void SetTimeScale(float scale)
        {
            TimeScale = scale;
            SetTimeScaleCallCount++;
        }

        public void ResetTimeScale()
        {
            TimeScale = 1f;
            ResetTimeScaleCallCount++;
        }

        public void Pause() => _pauseCount++;

        public void Resume()
        {
            if (_pauseCount > 0)
            {
                _pauseCount--;
            }
        }
    }
}
