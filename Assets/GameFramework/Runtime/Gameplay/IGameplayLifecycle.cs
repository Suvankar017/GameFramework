namespace GameFramework.Gameplay
{
    /// <summary>
    /// Optional contract for a gameplay participant that needs to react to the current gameplay
    /// session's lifecycle, driven by <see cref="IGameplayService"/>. Implementing this does not
    /// require inheriting from any framework base class — any plain class or MonoBehaviour can
    /// implement it and call <see cref="IGameplayService.RegisterLifecycle"/> itself.
    ///
    /// Phases, in order: <see cref="OnGameplayInitialize"/> (once, at registration) →
    /// <see cref="OnGameplayBeginPlay"/> (each time a session starts) → zero or more
    /// <see cref="OnGameplayPause"/>/<see cref="OnGameplayResume"/> pairs, driven automatically by
    /// <see cref="Runtime.Time.ITimeService.IsPaused"/> — never call these two yourself → back to
    /// <see cref="OnGameplayShutdown"/> when the session ends (or the participant unregisters).
    /// A participant may be registered again for a later session; <see cref="OnGameplayInitialize"/>
    /// is called only once total, the first time it registers.
    /// </summary>
    public interface IGameplayLifecycle
    {
        /// <summary>Called once, the first time this participant is registered.</summary>
        void OnGameplayInitialize();

        /// <summary>Called when a gameplay session becomes active — either immediately if the
        /// session is already playing at registration time, or when <see cref="IGameplayService.BeginPlay"/>
        /// is next called.</summary>
        void OnGameplayBeginPlay();

        /// <summary>Called when <see cref="Runtime.Time.ITimeService.IsPaused"/> becomes true while
        /// a session is active. Never called directly — pause is detected automatically from the
        /// Phase 2 Time service; nothing in this framework writes <c>Time.timeScale</c> itself.</summary>
        void OnGameplayPause();

        /// <summary>Called when <see cref="Runtime.Time.ITimeService.IsPaused"/> becomes false again
        /// while a session is active.</summary>
        void OnGameplayResume();

        /// <summary>Called when the current session ends (<see cref="IGameplayService.EndPlay"/>,
        /// or the service itself shutting down) or when this participant unregisters early. Use
        /// this to unsubscribe events, cancel owned timers, and release temporary state.</summary>
        void OnGameplayShutdown();
    }
}
