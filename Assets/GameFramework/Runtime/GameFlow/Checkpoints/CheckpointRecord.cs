using GameFramework.Gameplay.Objectives;

namespace GameFramework.GameFlow.Checkpoints
{
    /// <summary>One registered checkpoint's data, as stored by <see cref="CheckpointSystem"/>.
    /// Either or both of <see cref="Transform"/>/<see cref="Snapshot"/> may be supplied - a
    /// checkpoint is not forced to be transform-only.</summary>
    public readonly struct CheckpointRecord
    {
        public readonly string Id;
        public readonly CheckpointData? Transform;
        public readonly IGameplaySnapshot Snapshot;

        public CheckpointRecord(string id, CheckpointData? transform, IGameplaySnapshot snapshot)
        {
            Id = id;
            Transform = transform;
            Snapshot = snapshot;
        }
    }
}
