using GameFramework.Runtime.Services;

namespace GameFramework.Performance.Mobile
{
    /// <summary>
    /// Centralizes <c>OnApplicationPause</c>/<c>OnApplicationFocus</c>/<c>OnApplicationQuit</c> into
    /// one place and republishes them as Phase 2 events
    /// (<see cref="ApplicationPausedEvent"/>/<see cref="ApplicationResumedEvent"/>/
    /// <see cref="ApplicationFocusChangedEvent"/>/<see cref="ApplicationQuittingEvent"/>), so no
    /// other subsystem needs its own <c>MonoBehaviour</c> just to observe these. Subscribe via the
    /// existing <see cref="Runtime.Events.IEventService"/> - this interface exists only to give the
    /// relay a registered lifecycle of its own.
    /// </summary>
    public interface IApplicationLifecycleService : IGameService
    {
        bool IsPaused { get; }
        bool HasFocus { get; }
    }
}
