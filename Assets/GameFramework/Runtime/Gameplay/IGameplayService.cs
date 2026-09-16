using GameFramework.Runtime.Services;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Application-level gameplay loop coordinator — the only piece of Phase 4 infrastructure that
    /// needs to be a registered service (see <see cref="GameplayBootstrapper"/>). It does not
    /// duplicate Unity's PlayerLoop or Phase 1's <see cref="Runtime.State.IGameStateService"/>: it
    /// only gives gameplay participants predictable Initialize/BeginPlay/Pause/Resume/Shutdown
    /// hooks (<see cref="IGameplayLifecycle"/>) and three opt-in per-frame tick phases
    /// (<see cref="IGameplayTickable"/>/<see cref="IGameplayFixedTickable"/>/<see cref="IGameplayLateTickable"/>),
    /// so gameplay code isn't forced into a per-object <c>Update()</c>/<c>MonoBehaviour</c>
    /// dependency on this specific service.
    ///
    /// A game decides when a "gameplay session" starts/ends by calling <see cref="BeginPlay"/>/
    /// <see cref="EndPlay"/> — commonly from its own <see cref="Runtime.State.IGameState"/>
    /// implementation for whatever it calls its "Gameplay" state, since this framework defines no
    /// concrete states of its own. Pause/Resume are never called directly by a game — they are
    /// detected automatically from <see cref="Runtime.Time.ITimeService.IsPaused"/> every tick.
    /// </summary>
    public interface IGameplayService : IGameService
    {
        GameplayLoopState State { get; }

        /// <summary>True only while <see cref="State"/> is <see cref="GameplayLoopState.Playing"/>
        /// — the single check gameplay code needs for "should I simulate right now".</summary>
        bool IsActive { get; }

        /// <summary>Starts a gameplay session: calls <see cref="IGameplayLifecycle.OnGameplayBeginPlay"/>
        /// on every currently registered participant and begins ticking. A no-op (logged) if a
        /// session is already active.</summary>
        void BeginPlay();

        /// <summary>Ends the current gameplay session: calls <see cref="IGameplayLifecycle.OnGameplayShutdown"/>
        /// on every currently registered participant and stops ticking. Participants remain
        /// registered for a possible future <see cref="BeginPlay"/> (e.g. restarting a level). A
        /// no-op (logged) if no session is active.</summary>
        void EndPlay();

        /// <summary>Registers a lifecycle participant. Calls <see cref="IGameplayLifecycle.OnGameplayInitialize"/>
        /// once immediately, then immediately calls whatever further phase(s) match the loop's
        /// current <see cref="State"/> (BeginPlay, and Pause if currently paused) so a late joiner
        /// ends up in the same state as everyone else. Registering the same participant twice is a
        /// no-op (logged).</summary>
        void RegisterLifecycle(IGameplayLifecycle participant);

        /// <summary>Calls <see cref="IGameplayLifecycle.OnGameplayShutdown"/> once and removes the
        /// participant. Safe to call with a participant that isn't registered (no-op).</summary>
        void UnregisterLifecycle(IGameplayLifecycle participant);

        void RegisterTickable(IGameplayTickable tickable);
        void UnregisterTickable(IGameplayTickable tickable);

        void RegisterFixedTickable(IGameplayFixedTickable tickable);
        void UnregisterFixedTickable(IGameplayFixedTickable tickable);

        void RegisterLateTickable(IGameplayLateTickable tickable);
        void UnregisterLateTickable(IGameplayLateTickable tickable);
    }
}
