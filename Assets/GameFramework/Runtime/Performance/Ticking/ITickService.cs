using GameFramework.Runtime.Services;

namespace GameFramework.Performance.Ticking
{
    /// <summary>
    /// Centralized per-frame callback registry - register once instead of writing a
    /// <c>MonoBehaviour.Update</c> per object. See <see cref="ITickable"/>'s remarks for how this
    /// differs from <see cref="GameFramework.Gameplay.IGameplayService"/>'s gameplay-session ticking.
    ///
    /// Registration is safe to call redundantly (a second Register for an already-registered
    /// instance, or an Unregister for one that isn't registered, are both no-ops) and safe to call
    /// from inside a tick callback (see <see cref="TickRegistry{T}.Snapshot"/>).
    /// </summary>
    public interface ITickService : IGameService
    {
        /// <summary><paramref name="priority"/> - lower values tick first; default 0.
        /// <paramref name="group"/> is informational only (diagnostics), it does not affect
        /// execution order.</summary>
        void Register(ITickable tickable, int priority = 0, TickGroup group = TickGroup.Gameplay);
        void Unregister(ITickable tickable);

        void RegisterFixed(IFixedTickable tickable, int priority = 0, TickGroup group = TickGroup.Physics);
        void UnregisterFixed(IFixedTickable tickable);

        void RegisterLate(ILateTickable tickable, int priority = 0, TickGroup group = TickGroup.Presentation);
        void UnregisterLate(ILateTickable tickable);

        int TickableCount { get; }
        int FixedTickableCount { get; }
        int LateTickableCount { get; }
    }
}
