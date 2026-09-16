using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Gameplay.Tests
{
    /// <summary>Minimal controllable <see cref="ITimeService"/> for deterministic PlayMode
    /// Gameplay tests — duplicated from the EditMode test assembly's fake since test assemblies
    /// don't share internals across each other.</summary>
    internal sealed class FakeTimeService : ITimeService
    {
        public float ScaledDeltaTime { get; set; }
        public float UnscaledDeltaTime { get; set; }
        public float FixedDeltaTime { get; set; }
        public float UnscaledFixedDeltaTime { get; set; }
        public float ScaledTime { get; set; }
        public float UnscaledTime { get; set; }
        public float Realtime { get; set; }
        public float TimeScale { get; private set; } = 1f;
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
    }
}
