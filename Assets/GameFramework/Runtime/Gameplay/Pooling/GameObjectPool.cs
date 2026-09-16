using System;
using System.Collections.Generic;
using GameFramework.Core.Extensions;
using GameFramework.Core.Validation;
using GameFramework.Gameplay.Lifecycle;
using GameFramework.Runtime.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay.Pooling
{
    /// <summary>
    /// Reusable pool for one GameObject prefab. Built on <see cref="UnityEngine.Pool.ObjectPool{T}"/>
    /// (built into Unity since 2021.1 — this class adds the Unity-specific ergonomics on top:
    /// activation/deactivation, transform placement, an optional <see cref="IGameplayObjectLifecycle"/>
    /// hook per instance, and safety against externally-destroyed pooled instances) rather than
    /// reimplementing a free-list from scratch.
    ///
    /// Lifecycle per instance: <c>Get</c> → activates the GameObject and calls
    /// <see cref="IGameplayObjectLifecycle.Activate"/> on every component that implements it (if
    /// any — a prefab with none still pools correctly, it just gets SetActive(true)/(false)) →
    /// caller uses it → <c>Release</c> → calls <c>Deactivate</c> and deactivates the GameObject →
    /// stored inactive under this pool's own container until the next <c>Get</c>, or destroyed
    /// (calling <c>Dispose</c> first) if the pool is already at <see cref="GameObjectPoolConfig.MaxSize"/>.
    ///
    /// Ownership: every instance lives under a container Transform this pool creates (or one you
    /// supply). Use a <c>DontDestroyOnLoad</c> container (the default when none is supplied) for an
    /// application-lifetime pool; supply a scene-local container, and <see cref="Dispose"/> the pool
    /// when that scene unloads, for a scene-lifetime pool. Never destroy a pooled instance directly
    /// — always <see cref="Release"/> it — since an instance destroyed outside this pool can no
    /// longer be told apart from a valid one until the next <see cref="Get()"/> notices and
    /// transparently replaces it (logged as an error, since it means something bypassed the pool).
    /// </summary>
    public sealed class GameObjectPool : IDisposable
    {
        private const string LogCategory = "Pooling";

        private readonly ObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly bool _ownsContainer;
        private readonly bool _resetTransformOnRelease;
        private readonly List<IGameplayObjectLifecycle> _lifecycleScratch = new List<IGameplayObjectLifecycle>();

        public GameObjectPool(GameObject prefab, GameObjectPoolConfig config = null, Transform container = null)
        {
            _prefab = Guard.NotNull(prefab, nameof(prefab));
            config ??= new GameObjectPoolConfig();
            _resetTransformOnRelease = config.ResetTransformOnRelease;

            if (container == null)
            {
                var containerGo = new GameObject($"Pool ({prefab.name})");
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(containerGo);
                }

                container = containerGo.transform;
                _ownsContainer = true;
            }

            _container = container;

            _pool = new ObjectPool<GameObject>(
                createFunc: CreateInstance,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: true,
                defaultCapacity: Mathf.Max(0, config.DefaultCapacity),
                maxSize: Mathf.Max(1, config.MaxSize));

            if (config.PrewarmCount > 0)
            {
                Prewarm(config.PrewarmCount);
            }
        }

        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;
        public int CountAll => _pool.CountAll;

        /// <summary>Eagerly creates <paramref name="count"/> instances and immediately returns them
        /// to the pool. Each prewarmed instance goes through Activate/Deactivate exactly once during
        /// this call — <see cref="UnityEngine.Pool.ObjectPool{T}"/> has no lower-level way to seed
        /// its free list without going through Get/Release.</summary>
        public void Prewarm(int count)
        {
            if (count <= 0)
            {
                return;
            }

            var buffer = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        /// <summary>Gets an instance at the container's default placement — call the
        /// position/rotation overload to place it explicitly.</summary>
        public GameObject Get() => GetInternal(Vector3.zero, Quaternion.identity, null, applyTransform: false);

        /// <summary>Gets an instance and places it at <paramref name="position"/>/<paramref name="rotation"/>,
        /// optionally re-parenting it to <paramref name="parent"/> (world position preserved).</summary>
        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null) =>
            GetInternal(position, rotation, parent, applyTransform: true);

        private GameObject GetInternal(Vector3 position, Quaternion rotation, Transform parent, bool applyTransform)
        {
            GameObject instance = _pool.Get();
            if (instance.IsNullOrDestroyed())
            {
                Log.Error(LogCategory, $"A pooled instance of '{_prefab.name}' was destroyed outside the pool " +
                    "(always use Release, never Destroy, on a pooled instance); creating a replacement.");
                instance = CreateInstance();
                OnGet(instance);
            }

            if (applyTransform)
            {
                Transform instanceTransform = instance.transform;
                instanceTransform.SetPositionAndRotation(position, rotation);
                if (parent != null)
                {
                    instanceTransform.SetParent(parent, worldPositionStays: true);
                }
            }

            return instance;
        }

        /// <summary>Returns <paramref name="instance"/> to the pool. Logs (rather than throwing) on
        /// a null/already-destroyed instance, or a duplicate release of the same instance.</summary>
        public void Release(GameObject instance)
        {
            if (instance.IsNullOrDestroyed())
            {
                Log.Warning(LogCategory, "Release called with a null or already-destroyed instance; ignored.");
                return;
            }

            try
            {
                _pool.Release(instance);
            }
            catch (InvalidOperationException ex)
            {
                Log.Error(LogCategory, $"Duplicate Release detected for '{instance.name}': {ex.Message}");
            }
        }

        /// <summary>Destroys every currently inactive instance and this pool's own container (if it
        /// created one). Does not affect instances currently out via <see cref="Get()"/> — release
        /// those first if they should also be destroyed.</summary>
        public void Dispose()
        {
            _pool.Dispose();

            if (_ownsContainer && _container.IsAlive())
            {
                Object.Destroy(_container.gameObject);
            }
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Object.Instantiate(_prefab, _container);
            instance.SetActive(false);

            instance.GetComponents(_lifecycleScratch);
            for (int i = 0; i < _lifecycleScratch.Count; i++)
            {
                try
                {
                    _lifecycleScratch[i].Initialize();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, LogCategory, instance);
                }
            }

            return instance;
        }

        private void OnGet(GameObject instance)
        {
            if (instance.IsNullOrDestroyed())
            {
                // Popped a stack entry that was destroyed outside the pool. GetInternal notices
                // the pool still returned a destroyed reference and transparently replaces it -
                // this guard only prevents that path itself from throwing first.
                return;
            }

            instance.SetActive(true);

            instance.GetComponents(_lifecycleScratch);
            for (int i = 0; i < _lifecycleScratch.Count; i++)
            {
                try
                {
                    _lifecycleScratch[i].Activate();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, LogCategory, instance);
                }
            }
        }

        private void OnRelease(GameObject instance)
        {
            instance.GetComponents(_lifecycleScratch);
            for (int i = 0; i < _lifecycleScratch.Count; i++)
            {
                try
                {
                    _lifecycleScratch[i].Deactivate();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, LogCategory, instance);
                }
            }

            instance.SetActive(false);

            if (_resetTransformOnRelease)
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            instance.transform.SetParent(_container, worldPositionStays: false);
        }

        private void OnDestroyInstance(GameObject instance)
        {
            if (instance.IsNullOrDestroyed())
            {
                return;
            }

            instance.GetComponents(_lifecycleScratch);
            for (int i = 0; i < _lifecycleScratch.Count; i++)
            {
                try
                {
                    _lifecycleScratch[i].Dispose();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, LogCategory, instance);
                }
            }

            Object.Destroy(instance);
        }
    }
}
