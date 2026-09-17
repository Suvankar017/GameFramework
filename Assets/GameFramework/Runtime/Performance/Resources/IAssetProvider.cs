using System;
using Object = UnityEngine.Object;

namespace GameFramework.Performance.Resources
{
    /// <summary>
    /// Framework-level abstraction over runtime asset loading, so game systems can depend on this
    /// interface instead of a specific loading mechanism. The project does not currently use
    /// Addressables or AssetBundles (see <see cref="ResourcesAssetProvider"/>'s remarks), so the
    /// only shipped implementation wraps <see cref="UnityEngine.Resources"/> - if a project adopts
    /// Addressables later, it can implement this interface without its call sites changing.
    ///
    /// Every successful <see cref="Load{T}"/>/<see cref="LoadAsync{T}"/> call returns a handle that
    /// must be released via <see cref="IAssetHandle{T}.Release"/> - loading the same key twice
    /// without releasing keeps it referenced twice, and the underlying asset is only eligible for
    /// <see cref="UnityEngine.Resources.UnloadUnusedAssets"/> once every handle for it has been
    /// released. This provider does not unload anything automatically on its own schedule.
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>Synchronous load - blocks until the asset is available. Returns a handle whose
        /// <see cref="IAssetHandle{T}.IsLoaded"/> is false if <paramref name="key"/> could not be
        /// resolved to an asset of type <typeparamref name="T"/>.</summary>
        IAssetHandle<T> Load<T>(string key) where T : Object;

        /// <summary>
        /// Asynchronous load. <paramref name="owner"/> is optional - if supplied and destroyed
        /// before loading finishes, <paramref name="onLoaded"/> is never invoked (the in-flight
        /// operation still completes internally, but nothing calls back into a destroyed owner).
        /// </summary>
        void LoadAsync<T>(string key, Action<IAssetHandle<T>> onLoaded, Object owner = null) where T : Object;

        /// <summary>Current outstanding handle count for <paramref name="key"/> - 0 if nothing has
        /// it loaded. Diagnostic only; do not use this to decide whether to call
        /// <see cref="IAssetHandle{T}.Release"/> from unrelated code.</summary>
        int GetReferenceCount(string key);

        /// <summary>Releases every outstanding reference and clears the internal cache. Intended for
        /// scene-transition/shutdown cleanup, not routine use - prefer releasing individual handles.</summary>
        void ClearAll();
    }
}
