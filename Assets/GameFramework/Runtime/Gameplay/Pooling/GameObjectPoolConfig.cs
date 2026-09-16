namespace GameFramework.Gameplay.Pooling
{
    /// <summary>Plain-code configuration for a <see cref="GameObjectPool"/>. For designer-authored
    /// presets, see <see cref="PoolConfiguration"/> (a ScriptableObject that converts to this).</summary>
    public sealed class GameObjectPoolConfig
    {
        /// <summary>Initial backing capacity for the pool's internal free list. Not the same as
        /// prewarming — this alone allocates no instances.</summary>
        public int DefaultCapacity = 10;

        /// <summary>Maximum number of inactive instances the pool retains. An instance released
        /// beyond this is destroyed instead of stored — see Pool Maximum Size.</summary>
        public int MaxSize = 100;

        /// <summary>Number of instances to eagerly create (and immediately return to the pool) at
        /// construction. 0 (default) creates nothing until first use — prewarming only happens when
        /// explicitly requested.</summary>
        public int PrewarmCount;

        /// <summary>If true, an instance's local position/rotation are reset to identity when
        /// released back to the pool. Default false: a caller that intentionally controls transform
        /// state is never silently overridden.</summary>
        public bool ResetTransformOnRelease;
    }
}
