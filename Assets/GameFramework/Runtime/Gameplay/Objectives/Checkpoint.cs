using UnityEngine;

namespace GameFramework.Gameplay.Objectives
{
    /// <summary>Plain, serializable checkpoint payload. If a game needs this to survive save/load,
    /// persist it through the Phase 2 Persistence service — this is not a second save system.</summary>
    [System.Serializable]
    public struct CheckpointData
    {
        public string Id;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    /// <summary>Marks a scene location as a checkpoint. Carries data only — it does not respawn
    /// anything or decide when it was "reached"; that is game-specific (e.g. a trigger collider
    /// the game itself wires up, which then reads <see cref="GetData"/> and persists it).</summary>
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string _id;

        public string Id => _id;

        public CheckpointData GetData() => new CheckpointData
        {
            Id = _id,
            Position = transform.position,
            Rotation = transform.rotation
        };

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[Checkpoint] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
