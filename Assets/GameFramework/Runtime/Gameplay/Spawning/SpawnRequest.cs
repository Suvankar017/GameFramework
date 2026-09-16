using UnityEngine;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>What/where/how to spawn — the answer to "what should spawn, where, and with what
    /// parent". Deliberately does not answer "when" or "how many" (that's <see cref="Spawner"/>'s
    /// own limits/cooldown) or "how" (that's <see cref="ISpawnProvider"/>).</summary>
    public readonly struct SpawnRequest
    {
        public readonly GameObject Prefab;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Transform Parent;

        /// <summary>Optional caller-defined data an <see cref="ISpawnProvider"/> or the spawned
        /// object itself may use (e.g. a wave index, a faction). Not interpreted by the framework.</summary>
        public readonly object Context;

        public SpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, object context = null)
        {
            Prefab = prefab;
            Position = position;
            Rotation = rotation;
            Parent = parent;
            Context = context;
        }
    }
}
