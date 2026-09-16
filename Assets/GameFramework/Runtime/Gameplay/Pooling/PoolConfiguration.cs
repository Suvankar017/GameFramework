using UnityEngine;

namespace GameFramework.Gameplay.Pooling
{
    /// <summary>Designer-authored pool preset — pairs a prefab with the numeric settings a
    /// <see cref="GameObjectPool"/> needs. Carries no runtime state (do not mutate at runtime;
    /// create a <see cref="GameObjectPoolConfig"/> via <see cref="ToPoolConfig"/> per pool instead).</summary>
    [CreateAssetMenu(menuName = "GameFramework/Gameplay/Pool Configuration", fileName = "PoolConfiguration")]
    public sealed class PoolConfiguration : ScriptableObject
    {
        public GameObject Prefab;
        public int DefaultCapacity = 10;
        public int MaxSize = 100;
        public int PrewarmCount;
        public bool ResetTransformOnRelease;

        public GameObjectPoolConfig ToPoolConfig()
        {
            return new GameObjectPoolConfig
            {
                DefaultCapacity = DefaultCapacity,
                MaxSize = MaxSize,
                PrewarmCount = PrewarmCount,
                ResetTransformOnRelease = ResetTransformOnRelease
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Prefab == null)
            {
                Debug.LogWarning($"[PoolConfiguration] '{name}' has no Prefab assigned.", this);
            }

            if (MaxSize < 1)
            {
                Debug.LogWarning($"[PoolConfiguration] '{name}' has MaxSize < 1; nothing would ever be retained.", this);
            }

            if (DefaultCapacity > MaxSize)
            {
                Debug.LogWarning($"[PoolConfiguration] '{name}' has DefaultCapacity > MaxSize.", this);
            }

            if (PrewarmCount > MaxSize)
            {
                Debug.LogWarning($"[PoolConfiguration] '{name}' has PrewarmCount > MaxSize.", this);
            }
        }
#endif
    }
}
