using GameFramework.Gameplay.Pooling;
using UnityEngine;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>An <see cref="ISpawnProvider"/> backed by a <see cref="GameObjectPool"/> — swap
    /// this in for <see cref="InstantiateSpawnProvider"/> to make a <see cref="Spawner"/> pooled
    /// without changing any gameplay code that calls it.</summary>
    public sealed class PooledSpawnProvider : ISpawnProvider
    {
        private readonly GameObjectPool _pool;

        public PooledSpawnProvider(GameObjectPool pool)
        {
            _pool = pool;
        }

        public GameObject Spawn(SpawnRequest request)
        {
            if (_pool == null)
            {
                return null;
            }

            return _pool.Get(request.Position, request.Rotation, request.Parent);
        }

        public void Despawn(GameObject instance)
        {
            _pool?.Release(instance);
        }
    }
}
