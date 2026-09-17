using UnityEngine;

namespace GameFramework.Performance.Resources
{
    /// <summary>
    /// Ownership token for one <see cref="IAssetProvider.Load{T}"/>/<see cref="IAssetProvider.LoadAsync{T}"/>
    /// call. Whoever receives a handle owns exactly one reference and must call
    /// <see cref="Release"/> exactly once when done with it - see <see cref="IAssetProvider"/>'s
    /// remarks for what happens if that never happens.
    /// </summary>
    public interface IAssetHandle<out T> where T : Object
    {
        /// <summary>The loaded asset, or null if loading failed or this handle was already released.</summary>
        T Asset { get; }

        bool IsLoaded { get; }

        /// <summary>Releases this handle's reference. Safe to call more than once - a second call is
        /// a no-op, matching the framework's other release-style APIs
        /// (<see cref="GameFramework.Gameplay.Pooling.GameObjectPool.Release"/>).</summary>
        void Release();
    }
}
