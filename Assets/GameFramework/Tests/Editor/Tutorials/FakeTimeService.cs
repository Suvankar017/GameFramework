using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Tutorials.Tests
{
    /// <summary>See <c>GameFramework.GameFlow.Tests.FakeTimeService</c>'s remarks - same pattern,
    /// duplicated per test assembly since these are test-only internal helpers. <see cref="Pause"/>/
    /// <see cref="Resume"/> are reference-counted, matching the real <c>TimeService</c>'s contract -
    /// TutorialService's pause behavior depends on that being accurate.</summary>
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

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void SetTimeScale(float scale) => TimeScale = scale;
        public void ResetTimeScale() => TimeScale = 1f;

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
