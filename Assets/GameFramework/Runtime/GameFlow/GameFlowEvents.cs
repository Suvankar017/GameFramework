using GameFramework.GameFlow.Session;
using UnityEngine;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Published by <see cref="GameFlowService"/> through the Phase 2 Event System - no separate
    /// notification mechanism is introduced for this, matching every other cross-system
    /// notification in the framework. See the framework's Phase 8 documentation for the guaranteed
    /// publish order around completion/failure/restart.
    /// </summary>
    public readonly struct LevelFlowStateChangedEvent
    {
        public readonly LevelFlowState Previous;
        public readonly LevelFlowState Current;

        public LevelFlowStateChangedEvent(LevelFlowState previous, LevelFlowState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct LevelLoadingStartedEvent
    {
        public readonly LevelId LevelId;
        public LevelLoadingStartedEvent(LevelId levelId) => LevelId = levelId;
    }

    /// <summary>Published once the scene has finished loading, while <see cref="LevelFlowState.Initializing"/>
    /// is current - the moment game-specific setup (spawning, wiring references) should run.</summary>
    public readonly struct LevelInitializingEvent
    {
        public readonly LevelId LevelId;
        public LevelInitializingEvent(LevelId levelId) => LevelId = levelId;
    }

    public readonly struct LevelReadyEvent
    {
        public readonly LevelId LevelId;
        public LevelReadyEvent(LevelId levelId) => LevelId = levelId;
    }

    public readonly struct LevelStartedEvent
    {
        public readonly LevelId LevelId;
        public readonly SessionId SessionId;
        public readonly int AttemptNumber;

        public LevelStartedEvent(LevelId levelId, SessionId sessionId, int attemptNumber)
        {
            LevelId = levelId;
            SessionId = sessionId;
            AttemptNumber = attemptNumber;
        }
    }

    public readonly struct GameplaySessionStartedEvent
    {
        public readonly SessionId SessionId;
        public readonly LevelId LevelId;
        public readonly int AttemptNumber;

        public GameplaySessionStartedEvent(SessionId sessionId, LevelId levelId, int attemptNumber)
        {
            SessionId = sessionId;
            LevelId = levelId;
            AttemptNumber = attemptNumber;
        }
    }

    public readonly struct GameplaySessionPausedEvent
    {
        public readonly SessionId SessionId;
        public GameplaySessionPausedEvent(SessionId sessionId) => SessionId = sessionId;
    }

    public readonly struct GameplaySessionResumedEvent
    {
        public readonly SessionId SessionId;
        public GameplaySessionResumedEvent(SessionId sessionId) => SessionId = sessionId;
    }

    public readonly struct GameplaySessionCompletedEvent
    {
        public readonly SessionId SessionId;
        public readonly LevelId LevelId;
        public readonly int AttemptNumber;
        public readonly float ElapsedGameplayTime;
        public readonly GameplayResult Result;

        public GameplaySessionCompletedEvent(SessionId sessionId, LevelId levelId, int attemptNumber, float elapsedGameplayTime, GameplayResult result)
        {
            SessionId = sessionId;
            LevelId = levelId;
            AttemptNumber = attemptNumber;
            ElapsedGameplayTime = elapsedGameplayTime;
            Result = result;
        }
    }

    public readonly struct GameplaySessionFailedEvent
    {
        public readonly SessionId SessionId;
        public readonly LevelId LevelId;
        public readonly int AttemptNumber;
        public readonly float ElapsedGameplayTime;
        public readonly GameplayResult Result;

        public GameplaySessionFailedEvent(SessionId sessionId, LevelId levelId, int attemptNumber, float elapsedGameplayTime, GameplayResult result)
        {
            SessionId = sessionId;
            LevelId = levelId;
            AttemptNumber = attemptNumber;
            ElapsedGameplayTime = elapsedGameplayTime;
            Result = result;
        }
    }

    /// <summary>The player-facing "a level finished successfully" notification - Phase 6/7
    /// progression/statistics/objective/reward systems react to this (or to
    /// <see cref="GameplaySessionCompletedEvent"/>) rather than GameFlow calling into them
    /// directly. See <see cref="GameFlowService"/>'s remarks on not duplicating those systems.</summary>
    public readonly struct LevelCompletedEvent
    {
        public readonly LevelId LevelId;
        public readonly SessionId SessionId;
        public readonly int AttemptNumber;
        public readonly float CompletionTime;
        public readonly GameplayResult Result;

        public LevelCompletedEvent(LevelId levelId, SessionId sessionId, int attemptNumber, float completionTime, GameplayResult result)
        {
            LevelId = levelId;
            SessionId = sessionId;
            AttemptNumber = attemptNumber;
            CompletionTime = completionTime;
            Result = result;
        }
    }

    public readonly struct LevelFailedEvent
    {
        public readonly LevelId LevelId;
        public readonly SessionId SessionId;
        public readonly int AttemptNumber;
        public readonly GameplayResult Result;

        public LevelFailedEvent(LevelId levelId, SessionId sessionId, int attemptNumber, GameplayResult result)
        {
            LevelId = levelId;
            SessionId = sessionId;
            AttemptNumber = attemptNumber;
            Result = result;
        }
    }

    /// <summary>Published once, the moment a restart is requested - before the new attempt exists.
    /// The new attempt's own <see cref="GameplaySessionStartedEvent"/>/<see cref="LevelStartedEvent"/>
    /// follow once it actually begins.</summary>
    public readonly struct LevelRestartedEvent
    {
        public readonly LevelId LevelId;
        public LevelRestartedEvent(LevelId levelId) => LevelId = levelId;
    }

    public readonly struct LevelExitedEvent
    {
        public readonly LevelId LevelId;
        public LevelExitedEvent(LevelId levelId) => LevelId = levelId;
    }

    public readonly struct CheckpointActivatedEvent
    {
        public readonly SessionId SessionId;
        public readonly string CheckpointId;

        public CheckpointActivatedEvent(SessionId sessionId, string checkpointId)
        {
            SessionId = sessionId;
            CheckpointId = checkpointId;
        }
    }

    /// <summary>Published by <see cref="IGameFlowService.Respawn"/>. GameFlow does not know what
    /// "the player" is - a game's own player controller subscribes to this and moves/resets itself;
    /// see <see cref="GameFlowService"/>'s remarks on respawn ownership.</summary>
    public readonly struct PlayerRespawnedEvent
    {
        public readonly SessionId SessionId;
        public readonly string CheckpointId;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        /// <summary>False if the active checkpoint carried no transform data (a pure
        /// <see cref="Checkpoints.IGameplaySnapshot"/>-only checkpoint) - <see cref="Position"/>/
        /// <see cref="Rotation"/> are meaningless in that case.</summary>
        public readonly bool HasTransform;

        public PlayerRespawnedEvent(SessionId sessionId, string checkpointId, Vector3 position, Quaternion rotation, bool hasTransform)
        {
            SessionId = sessionId;
            CheckpointId = checkpointId;
            Position = position;
            Rotation = rotation;
            HasTransform = hasTransform;
        }
    }
}
