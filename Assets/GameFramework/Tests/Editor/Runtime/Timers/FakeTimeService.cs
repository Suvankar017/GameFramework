using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Runtime.Tests.Timers
{
    /// <summary>Deterministic ITimeService double for timer tests — Tick()-driven behavior can be
    /// asserted without any real-time waiting.</summary>
    internal sealed class FakeTimeService : ITimeService
    {
        public float ScaledDeltaTime { get; set; }
        public float UnscaledDeltaTime { get; set; }
        public float FixedDeltaTime { get; set; }
        public float UnscaledFixedDeltaTime { get; set; }
        public float ScaledTime { get; set; }
        public float UnscaledTime { get; set; }
        public float Realtime { get; set; }
        public float TimeScale { get; set; } = 1f;
        public bool IsPaused { get; private set; }

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public void SetTimeScale(float scale) => TimeScale = scale;
        public void ResetTimeScale() => TimeScale = 1f;
        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        /// <summary>Sets both scaled and unscaled delta time to the same value for convenience.</summary>
        public void Advance(float delta)
        {
            ScaledDeltaTime = delta;
            UnscaledDeltaTime = delta;
        }
    }
}
