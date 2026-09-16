using UnityEngine;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>
    /// A creation strategy a <see cref="Spawner"/> delegates to — plain <see cref="Object.Instantiate"/>
    /// (<see cref="InstantiateSpawnProvider"/>) or a pool (<see cref="PooledSpawnProvider"/>). Game
    /// code that spawns through a <see cref="Spawner"/> never needs to know or change which one is
    /// in use.
    /// </summary>
    public interface ISpawnProvider
    {
        /// <summary>Returns the spawned instance, or null if creation failed (the caller treats a
        /// null return as <see cref="SpawnFailureReason.ProviderFailed"/> — this method itself
        /// should not throw for an expected failure).</summary>
        GameObject Spawn(SpawnRequest request);

        /// <summary>Returns <paramref name="instance"/> to wherever this provider gets instances
        /// from (destroys it, or releases it to a pool).</summary>
        void Despawn(GameObject instance);
    }
}
