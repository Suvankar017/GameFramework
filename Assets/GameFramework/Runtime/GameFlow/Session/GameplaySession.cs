using System;
using GameFramework.GameFlow.Checkpoints;

namespace GameFramework.GameFlow.Session
{
    /// <summary>
    /// One active gameplay attempt - a race, a puzzle attempt, a level run, a match. Generic on
    /// purpose: no lap count, no health, no score lives here, only the concepts every game genre
    /// shares (identity, attempt number, elapsed time, result, checkpoints).
    ///
    /// Exactly one session is ever active per <see cref="GameFlowService"/> - mutation methods are
    /// internal so that invariant is enforced by construction rather than convention; game code only
    /// ever reads a session through <see cref="IGameFlowService.CurrentSession"/>. A new attempt
    /// (<see cref="IGameFlowService.Retry"/>) always creates a new <see cref="GameplaySession"/>
    /// instance with a new <see cref="Id"/> and an incremented <see cref="AttemptNumber"/>;
    /// <see cref="IGameFlowService.Respawn"/> is the one operation that continues the *same*
    /// session/attempt instead - see that method's remarks.
    /// </summary>
    public sealed class GameplaySession : IDisposable
    {
        public SessionId Id { get; }
        public LevelId LevelId { get; }
        public int AttemptNumber { get; }

        /// <summary>Free-form game mode (Campaign/TimeTrial/Practice/...), inherited from the
        /// <see cref="LevelDefinition"/> unless overridden when the level was loaded. Context/data
        /// only.</summary>
        public string Mode { get; }

        /// <summary><see cref="Runtime.Time.ITimeService.Realtime"/> at creation - wall-clock, for
        /// diagnostics only. Use <see cref="ElapsedGameplayTime"/> for anything gameplay-facing.</summary>
        public float StartRealtime { get; }

        public GameplaySessionState State { get; private set; } = GameplaySessionState.Created;

        /// <summary>Meaningful once <see cref="State"/> is Completed/Failed/Aborted.</summary>
        public GameplayResult Result { get; private set; } = GameplayResult.None;

        /// <summary>Accumulated <see cref="Runtime.Time.ITimeService.ScaledDeltaTime"/> while this
        /// session was <see cref="GameplaySessionState.Active"/> - never advances while Paused,
        /// Created, or after ending, so it reflects genuine simulated gameplay time.</summary>
        public float ElapsedGameplayTime { get; private set; }

        public int PauseCount { get; private set; }

        /// <summary>How many times <see cref="IGameFlowService.Respawn"/> resumed this same
        /// session/attempt.</summary>
        public int RespawnCount { get; private set; }

        /// <summary>Owned by this session - checkpoint data never leaks into a later, unrelated
        /// session/level (see <see cref="Dispose"/>). Checkpoint ownership is deliberately
        /// session-scoped, not global; see <see cref="CheckpointSystem"/>'s remarks.</summary>
        public CheckpointSystem Checkpoints { get; } = new CheckpointSystem();

        internal GameplaySession(LevelId levelId, int attemptNumber, string mode, float startRealtime)
        {
            Id = SessionId.New();
            LevelId = levelId;
            AttemptNumber = attemptNumber;
            Mode = mode;
            StartRealtime = startRealtime;
        }

        internal void Start() => State = GameplaySessionState.Active;

        internal void Pause()
        {
            if (State != GameplaySessionState.Active)
            {
                return;
            }

            State = GameplaySessionState.Paused;
            PauseCount++;
        }

        internal void Resume()
        {
            if (State != GameplaySessionState.Paused)
            {
                return;
            }

            State = GameplaySessionState.Active;
        }

        internal void AccumulateTime(float scaledDeltaTime)
        {
            if (State == GameplaySessionState.Active)
            {
                ElapsedGameplayTime += scaledDeltaTime;
            }
        }

        internal void Complete(GameplayResult result)
        {
            State = GameplaySessionState.Completed;
            Result = result;
        }

        internal void Fail(GameplayResult result)
        {
            State = GameplaySessionState.Failed;
            Result = result;
        }

        internal void Abort(GameplayResult result)
        {
            State = GameplaySessionState.Aborted;
            Result = result;
        }

        /// <summary>Undoes <see cref="Fail"/> for <see cref="IGameFlowService.Respawn"/> - the same
        /// attempt resumes rather than a new one being created.</summary>
        internal void Revive()
        {
            State = GameplaySessionState.Active;
            Result = GameplayResult.None;
        }

        internal void NotifyRespawn() => RespawnCount++;

        public void Dispose()
        {
            if (State == GameplaySessionState.Disposed)
            {
                return;
            }

            Checkpoints.Clear();
            State = GameplaySessionState.Disposed;
        }
    }
}
