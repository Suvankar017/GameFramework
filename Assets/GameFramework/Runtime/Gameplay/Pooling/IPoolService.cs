using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Gameplay.Pooling
{
    /// <summary>
    /// Optional registry of named <see cref="GameObjectPool"/>s, for sharing one pool across
    /// systems that don't hold a direct reference to each other (e.g. "the projectile pool",
    /// looked up by key from wherever it's needed). Entirely optional — a <see cref="Spawner"/> or
    /// any other code can just construct and own a <see cref="GameObjectPool"/> directly without
    /// ever touching this service. Registering here makes a pool application-lifetime (it is
    /// disposed when this service shuts down); a scene-lifetime pool should not be registered here —
    /// construct and dispose it directly alongside the scene that owns it instead.
    /// </summary>
    public interface IPoolService : IGameService
    {
        /// <summary>Returns the pool registered under <paramref name="key"/>, creating it with
        /// <paramref name="config"/> if it doesn't exist yet. <paramref name="config"/> is ignored
        /// if the pool already exists.</summary>
        GameObjectPool GetOrCreate(string key, GameObject prefab, GameObjectPoolConfig config = null);

        bool TryGetPool(string key, out GameObjectPool pool);

        /// <summary>Disposes and removes the pool registered under <paramref name="key"/>. A no-op
        /// if none is registered under that key.</summary>
        void DisposePool(string key);
    }
}
