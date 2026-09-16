using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Gameplay.Pooling
{
    /// <summary>Default <see cref="IPoolService"/>. Owns every pool it creates and disposes them
    /// all on <see cref="Shutdown"/> — see <see cref="IPoolService"/> for when to use this versus
    /// constructing a <see cref="GameObjectPool"/> directly.</summary>
    public sealed class PoolService : IPoolService
    {
        private readonly Dictionary<string, GameObjectPool> _pools = new Dictionary<string, GameObjectPool>();

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
            foreach (GameObjectPool pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }

        public GameObjectPool GetOrCreate(string key, GameObject prefab, GameObjectPoolConfig config = null)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNull(prefab, nameof(prefab));

            if (_pools.TryGetValue(key, out GameObjectPool existing))
            {
                return existing;
            }

            var pool = new GameObjectPool(prefab, config);
            _pools.Add(key, pool);
            return pool;
        }

        public bool TryGetPool(string key, out GameObjectPool pool)
        {
            if (string.IsNullOrEmpty(key))
            {
                pool = null;
                return false;
            }

            return _pools.TryGetValue(key, out pool);
        }

        public void DisposePool(string key)
        {
            if (string.IsNullOrEmpty(key) || !_pools.TryGetValue(key, out GameObjectPool pool))
            {
                return;
            }

            pool.Dispose();
            _pools.Remove(key);
        }
    }
}
