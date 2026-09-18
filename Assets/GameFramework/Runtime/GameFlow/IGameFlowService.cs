using System;
using System.Collections.Generic;
using GameFramework.GameFlow.Checkpoints;
using GameFramework.GameFlow.Session;
using GameFramework.Gameplay.Objectives;
using GameFramework.Runtime.Services;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Application-level game-flow orchestrator - the Phase 8 equivalent of
    /// <see cref="Gameplay.IGameplayService"/>/<c>IQuestService</c>: one narrow, registered service
    /// that owns level-load lifecycle, the current <see cref="Session.GameplaySession"/>, and
    /// pause/checkpoint/respawn/restart commands. It deliberately does not own scene content, UI,
    /// audio, input, economy, or progression - see <see cref="GameFlowService"/>'s remarks for what
    /// it delegates to (Phase 2's <see cref="Runtime.SceneManagement.ISceneService"/>/
    /// <see cref="Runtime.Time.ITimeService"/>/<see cref="Runtime.Events.IEventService"/>, Phase 4's
    /// <see cref="Gameplay.IGameplayService"/>) versus what it only ever notifies about via events
    /// (Phase 6/7 progression/rewards/objectives/quests).
    ///
    /// Commands never throw for an invalid or currently-disallowed request - every one returns a
    /// result describing what happened instead, so normal gameplay flow (a player mashing "pause"
    /// twice, a duplicate "restart" tap) never needs a try/catch.
    /// </summary>
    public interface IGameFlowService : IGameService
    {
        LevelFlowState State { get; }

        /// <summary>Bounded, development-oriented transition history - see
        /// <see cref="LevelFlowStateMachine.History"/>.</summary>
        IReadOnlyList<LevelFlowState> StateHistory { get; }

        /// <summary>The level currently loaded/loading, or null while <see cref="LevelFlowState.Unloaded"/>.</summary>
        LevelDefinition CurrentLevel { get; }

        /// <summary>The active attempt, or null before <see cref="StartLevel"/> has ever been
        /// called for the current level (or after <see cref="ExitLevel"/>).</summary>
        GameplaySession CurrentSession { get; }

        /// <summary>True while any <see cref="PauseGameplay"/> token - or any other system entirely,
        /// e.g. the Phase 5 application-focus handler - has <see cref="Runtime.Time.ITimeService"/>
        /// paused. Single source of truth; this service tracks no separate pause flag of its own.</summary>
        bool IsPaused { get; }

        /// <summary>Free-form reasons currently keeping gameplay paused (see
        /// <see cref="PauseGameplay"/>) - development/UI diagnostic only.</summary>
        IReadOnlyCollection<string> ActivePauseReasons { get; }

        /// <summary>When true, the most recently activated checkpoint's transform is persisted
        /// through <see cref="Runtime.Persistence.IPersistenceService"/> so <see cref="TryLoadPersistedCheckpoint"/>
        /// can offer it again after an application restart. Off by default - see
        /// <see cref="GameFlowService"/>'s remarks on checkpoint persistence.</summary>
        bool PersistCheckpoints { get; set; }

        /// <summary>Raised after every <see cref="LevelFlowState"/> transition, as (previous, current) -
        /// the same information published as <see cref="LevelFlowStateChangedEvent"/>, offered
        /// directly for code that already holds this service rather than subscribing through
        /// <see cref="Runtime.Events.IEventService"/>.</summary>
        event Action<LevelFlowState, LevelFlowState> StateChanged;

        /// <summary>
        /// Starts loading <paramref name="level"/>'s scene. Only valid while
        /// <see cref="LevelFlowState.Unloaded"/> - a second concurrent load request is rejected
        /// (see <see cref="LevelLoadResult.Rejected"/>), matching the "one active level flow at a
        /// time" policy documented on <see cref="GameFlowService"/>. Loading is asynchronous; watch
        /// <see cref="LevelReadyEvent"/> or <see cref="StateChanged"/> for completion.
        /// </summary>
        LevelLoadResult LoadLevel(LevelDefinition level, string mode = null);

        /// <summary>Starts gameplay: creates and starts a new <see cref="Session.GameplaySession"/>
        /// (a new attempt for this level) and transitions to <see cref="LevelFlowState.Playing"/>.
        /// Only valid from <see cref="LevelFlowState.Ready"/>.</summary>
        TransitionResult StartLevel();

        /// <summary>Finalizes the current attempt as successful. Only valid from
        /// <see cref="LevelFlowState.Playing"/> - resume first if paused. Omit <paramref name="result"/>
        /// (or pass <see cref="GameplayResult.None"/>) for a plain <see cref="GameplayResult.Success"/>.</summary>
        TransitionResult CompleteLevel(GameplayResult result = default);

        /// <summary>Finalizes the current attempt as failed. Only valid from
        /// <see cref="LevelFlowState.Playing"/>. Omit <paramref name="result"/> for a plain
        /// <see cref="GameplayResult.Failure"/>.</summary>
        TransitionResult FailLevel(GameplayResult result = default);

        /// <summary>
        /// Starts a new attempt at the current level - "Restart"/"Retry" are the same operation
        /// here (see <see cref="GameFlowService"/>'s remarks distinguishing this from
        /// <see cref="Respawn"/>). Valid from Playing/Paused/Completed/Failed.
        /// <paramref name="reloadScene"/> (default true) reloads the level's scene first
        /// (Restarting → Loading → ... → Playing); false resets straight back to Playing without
        /// touching the scene.
        /// </summary>
        TransitionResult Retry(bool reloadScene = true);

        /// <summary>
        /// Continues the *current* attempt from its active checkpoint - increments
        /// <see cref="Session.GameplaySession.RespawnCount"/> rather than starting a new attempt,
        /// and publishes <see cref="PlayerRespawnedEvent"/> for game code to actually move/reset
        /// itself. Valid from <see cref="LevelFlowState.Playing"/> (a checkpoint reset without
        /// dying) or <see cref="LevelFlowState.Failed"/> (the common "died, respawn" case). Fails
        /// deterministically (<see cref="RespawnResult.NoCheckpoint"/>) rather than leaving anything
        /// half-restored if no checkpoint is active - see <see cref="RespawnResult"/>.
        /// </summary>
        RespawnResult Respawn();

        /// <summary>Tears down the current level/session (aborting an active attempt, if any) and
        /// returns to <see cref="LevelFlowState.Unloaded"/>. Valid from every state except the
        /// transient system-driven ones. Does not load any other scene - where to go next (a menu,
        /// a hub) is the game's decision.</summary>
        TransitionResult ExitLevel();

        /// <summary>
        /// Requests a gameplay pause for <paramref name="reason"/> (free-form, e.g. "Tutorial",
        /// "Dialog") - forces <see cref="Runtime.Time.ITimeService"/> to 0 for as long as this (or
        /// any other) token remains outstanding. Release the returned token to end this specific
        /// request; gameplay only actually resumes once every outstanding token (and every other
        /// <see cref="Runtime.Time.ITimeService.Pause"/> caller) has released. <see cref="LevelFlowState"/>
        /// transitions Playing↔Paused reactively as a result - never call a state transition
        /// directly for this.
        /// </summary>
        IPauseToken PauseGameplay(string reason = null);

        /// <summary>Registers (or overwrites) a checkpoint's reachable data on the current session.
        /// No-op (returns false) if no session is active.</summary>
        bool RegisterCheckpoint(string id, CheckpointData? transform = null, IGameplaySnapshot snapshot = null);

        /// <summary>Activates a previously registered checkpoint as the current respawn point and
        /// publishes <see cref="CheckpointActivatedEvent"/>. False if no session is active or
        /// <paramref name="id"/> was never registered.</summary>
        bool ActivateCheckpoint(string id);

        /// <summary>
        /// True if a checkpoint was persisted (see <see cref="PersistCheckpoints"/>) for the
        /// current <see cref="CurrentLevel"/> in a previous session. Purely informational - it does
        /// not register or activate anything; call <see cref="RegisterCheckpoint"/>/
        /// <see cref="ActivateCheckpoint"/> yourself if the game decides to offer it. Only the
        /// checkpoint's built-in transform is ever persisted, never an <see cref="IGameplaySnapshot"/>'s
        /// contents.
        /// </summary>
        bool TryLoadPersistedCheckpoint(out string checkpointId, out CheckpointData? transform);
    }
}
