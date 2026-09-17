using System;
using System.Collections.Generic;
using GameFramework.Core.Extensions;
using GameFramework.Performance.Profiling;
using GameFramework.Runtime.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Performance.Resources
{
    /// <summary>
    /// Default <see cref="IAssetProvider"/>, backed by <see cref="UnityEngine.Resources"/>. The
    /// project does not use Addressables or AssetBundles anywhere today, so this is the only
    /// provider shipped - per the framework's resource strategy, Addressables is not introduced
    /// speculatively (see the Resource Rules section of the framework documentation).
    ///
    /// Reference-counted per key: a second <see cref="Load{T}"/> for an already-loaded key returns
    /// a handle to the same cached asset and increments the count instead of loading again; the
    /// entry is dropped from the cache once every handle for that key has called
    /// <see cref="IAssetHandle{T}.Release"/>. Dropping a cache entry does not force-unload a
    /// GameObject/Component asset (<see cref="UnityEngine.Resources.UnloadAsset"/> does not support
    /// those types) - those remain eligible for the next
    /// <see cref="UnityEngine.Resources.UnloadUnusedAssets"/>, exactly like any other unreferenced
    /// Resources asset.
    /// </summary>
    public sealed class ResourcesAssetProvider : IAssetProvider
    {
        private const string LogCategory = "Resources";

        private sealed class CacheEntry
        {
            public Object Asset;
            public int RefCount;
        }

        private readonly Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();

        public IAssetHandle<T> Load<T>(string key) where T : Object
        {
            using (new ProfileScope(ProfilingCategory.Loading))
            {
                if (_cache.TryGetValue(key, out CacheEntry entry))
                {
                    entry.RefCount++;
                    return new AssetHandle<T>(this, key, entry.Asset as T);
                }

                T asset = UnityEngine.Resources.Load<T>(key);
                if (asset == null)
                {
                    Log.Warning(LogCategory, $"Resources.Load<{typeof(T).Name}>('{key}') found nothing.");
                    return new AssetHandle<T>(this, key, null);
                }

                _cache[key] = new CacheEntry { Asset = asset, RefCount = 1 };
                return new AssetHandle<T>(this, key, asset);
            }
        }

        public void LoadAsync<T>(string key, Action<IAssetHandle<T>> onLoaded, Object owner = null) where T : Object
        {
            if (_cache.TryGetValue(key, out CacheEntry existing))
            {
                existing.RefCount++;
                onLoaded?.Invoke(new AssetHandle<T>(this, key, existing.Asset as T));
                return;
            }

            bool hasOwner = owner != null; // Captured now - a destroyed Object also equals null, so
                                            // this must be recorded before that can happen, not
                                            // re-derived from `owner` at completion time.
            ResourceRequest request = UnityEngine.Resources.LoadAsync<T>(key);
            request.completed += _ => OnAsyncLoadCompleted(request, key, onLoaded, owner, hasOwner);
        }

        private void OnAsyncLoadCompleted<T>(ResourceRequest request, string key, Action<IAssetHandle<T>> onLoaded, Object owner, bool hasOwner) where T : Object
        {
            T asset = request.asset as T;
            if (asset != null)
            {
                _cache[key] = new CacheEntry { Asset = asset, RefCount = 1 };
            }
            else
            {
                Log.Warning(LogCategory, $"Resources.LoadAsync<{typeof(T).Name}>('{key}') found nothing.");
            }

            if (hasOwner && owner.IsNullOrDestroyed())
            {
                return; // Owner was destroyed while loading - never callback into it.
            }

            onLoaded?.Invoke(new AssetHandle<T>(this, key, asset));
        }

        public int GetReferenceCount(string key) => _cache.TryGetValue(key, out CacheEntry entry) ? entry.RefCount : 0;

        public void ClearAll()
        {
            foreach (KeyValuePair<string, CacheEntry> pair in _cache)
            {
                if (pair.Value.Asset != null && !(pair.Value.Asset is GameObject) && !(pair.Value.Asset is Component))
                {
                    UnityEngine.Resources.UnloadAsset(pair.Value.Asset);
                }
            }

            _cache.Clear();
        }

        internal void ReleaseKey(string key)
        {
            if (string.IsNullOrEmpty(key) || !_cache.TryGetValue(key, out CacheEntry entry))
            {
                return;
            }

            entry.RefCount--;
            if (entry.RefCount <= 0)
            {
                _cache.Remove(key);
            }
        }
    }
}
