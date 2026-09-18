using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Gameplay.Objectives;

namespace GameFramework.GameFlow.Checkpoints
{
    /// <summary>
    /// Session-scoped registry of reachable checkpoints and which one is currently active for
    /// respawn. Owned by exactly one <see cref="Session.GameplaySession"/> (see
    /// <see cref="Session.GameplaySession.Checkpoints"/>) - checkpoint state is transient by
    /// construction, never shared across sessions/levels, unless a game explicitly persists it
    /// itself (see <see cref="GameFlowService"/>'s remarks on checkpoint persistence).
    ///
    /// Carries data only, exactly like Phase 4's <see cref="Checkpoint"/> marker it composes with -
    /// this does not move any object, does not know what "the player" is, and is not itself a
    /// MonoBehaviour. A game typically registers one entry per <see cref="Checkpoint"/> placed in
    /// the scene (from <see cref="Checkpoint.GetData"/>) and calls <see cref="Activate"/> when its
    /// own trigger/collector code decides the checkpoint was reached.
    /// </summary>
    public sealed class CheckpointSystem
    {
        private readonly Dictionary<string, CheckpointRecord> _checkpoints = new Dictionary<string, CheckpointRecord>();

        public string CurrentId { get; private set; }
        public bool HasCurrent => CurrentId != null;

        /// <summary>Registers or overwrites a checkpoint's reachable data. Does not activate it.</summary>
        public void Register(string id, CheckpointData? transform = null, IGameplaySnapshot snapshot = null)
        {
            Guard.NotNullOrEmpty(id, nameof(id));
            _checkpoints[id] = new CheckpointRecord(id, transform, snapshot);
        }

        /// <summary>Marks <paramref name="id"/> as the current respawn point. Returns false (no
        /// state change) if <paramref name="id"/> was never registered.</summary>
        public bool Activate(string id)
        {
            if (string.IsNullOrEmpty(id) || !_checkpoints.ContainsKey(id))
            {
                return false;
            }

            CurrentId = id;
            return true;
        }

        public bool TryGetCurrent(out CheckpointRecord record)
        {
            if (CurrentId != null && _checkpoints.TryGetValue(CurrentId, out record))
            {
                return true;
            }

            record = default;
            return false;
        }

        /// <summary>Clears the active checkpoint (not the registry) - the next respawn attempt has
        /// nowhere to go until another checkpoint is activated.</summary>
        public void Reset() => CurrentId = null;

        /// <summary>Clears every registered checkpoint and the active one. Called automatically when
        /// the owning session ends (<see cref="Session.GameplaySession.Dispose"/>) so checkpoint data
        /// never leaks into an unrelated level/session.</summary>
        public void Clear()
        {
            _checkpoints.Clear();
            CurrentId = null;
        }
    }
}
