using System;
using System.Collections.Generic;
using GameFramework.Core.Extensions;
using GameFramework.Performance.Profiling;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>
    /// Generic spawner: answers what/where/when/how-many, and delegates the actual creation
    /// strategy to an <see cref="ISpawnProvider"/> (plain Instantiate by default; swap in a
    /// <see cref="PooledSpawnProvider"/> for pooling — gameplay code calling <see cref="TrySpawn()"/>
    /// never needs to change either way). Scene/object lifetime — not a service; a scene can have
    /// as many of these as it needs.
    ///
    /// Never decides what the spawned object does — that is the spawned prefab's own concern.
    /// </summary>
    public sealed class Spawner : MonoBehaviour
    {
        private const string LogCategory = "Spawning";

        [SerializeField] private SpawnConfiguration _configuration;

        [Header("Used when Configuration is not assigned")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _maxActiveInstances;
        [SerializeField] private int _maxTotalInstances;
        [SerializeField] private float _spawnCooldownSeconds;

        [Tooltip("Spawn points to cycle through round-robin. Empty uses this transform.")]
        [SerializeField] private Transform[] _spawnPoints = Array.Empty<Transform>();

        private readonly List<GameObject> _activeInstances = new List<GameObject>();
        private ISpawnProvider _provider = new InstantiateSpawnProvider();
        private ITimeService _time;
        private ILoggingService _log;
        private int _nextSpawnPointIndex;
        private int _totalSpawnedCount;
        private float _lastSpawnTime = float.NegativeInfinity;
        private bool _explicitlyInitialized;

        public event Action<GameObject> Spawned;
        public event Action<GameObject> Despawned;

        public int ActiveCount => _activeInstances.Count;
        public int TotalSpawnedCount => _totalSpawnedCount;

        /// <summary>Explicit dependency injection for testability/determinism — call before the
        /// first <see cref="TrySpawn()"/> if you don't want the lazy Bootstrap fallback below.</summary>
        public void Initialize(ITimeService time, ILoggingService log = null)
        {
            _time = time;
            _log = log;
            _explicitlyInitialized = true;
        }

        /// <summary>Swaps the creation strategy — e.g. a <see cref="PooledSpawnProvider"/> to make
        /// this spawner pooled.</summary>
        public void SetProvider(ISpawnProvider provider)
        {
            _provider = provider ?? new InstantiateSpawnProvider();
        }

        public SpawnResult TrySpawn()
        {
            Transform point = NextSpawnPoint();
            return TrySpawn(point.position, point.rotation);
        }

        public SpawnResult TrySpawn(Vector3 position, Quaternion rotation)
        {
            GameObject prefab = _configuration != null ? _configuration.Prefab : _prefab;
            return TrySpawn(new SpawnRequest(prefab, position, rotation));
        }

        public SpawnResult TrySpawn(SpawnRequest request)
        {
            using var _ = new ProfileScope(ProfilingCategory.Spawning);

            if (!isActiveAndEnabled)
            {
                return Fail(SpawnFailureReason.SpawnerDestroyed);
            }

            if (request.Prefab == null)
            {
                LogFailure("no prefab configured.");
                return Fail(SpawnFailureReason.MissingPrefab);
            }

            if (_provider == null)
            {
                LogFailure("no spawn provider configured.");
                return Fail(SpawnFailureReason.InvalidProvider);
            }

            PruneDestroyedActives();
            EnsureTimeService();

            float cooldown = _configuration != null ? _configuration.SpawnCooldownSeconds : _spawnCooldownSeconds;
            if (cooldown > 0f && _time != null && _time.ScaledTime - _lastSpawnTime < cooldown)
            {
                return Fail(SpawnFailureReason.Cooldown);
            }

            int maxActive = _configuration != null ? _configuration.MaxActiveInstances : _maxActiveInstances;
            if (maxActive > 0 && _activeInstances.Count >= maxActive)
            {
                return Fail(SpawnFailureReason.LimitReached);
            }

            int maxTotal = _configuration != null ? _configuration.MaxTotalInstances : _maxTotalInstances;
            if (maxTotal > 0 && _totalSpawnedCount >= maxTotal)
            {
                return Fail(SpawnFailureReason.LimitReached);
            }

            GameObject instance = _provider.Spawn(request);
            if (instance == null)
            {
                LogFailure($"provider '{_provider.GetType().Name}' returned null.");
                return Fail(SpawnFailureReason.ProviderFailed);
            }

            _activeInstances.Add(instance);
            _totalSpawnedCount++;
            _lastSpawnTime = _time != null ? _time.ScaledTime : 0f;

            Spawned?.Invoke(instance);
            return SpawnResult.Ok(instance);
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!_activeInstances.Remove(instance))
            {
                _log?.Log(LogLevel.Warning, LogCategory, $"Despawn called on '{instance.name}', which this spawner is not tracking.", this);
            }

            _provider?.Despawn(instance);
            Despawned?.Invoke(instance);
        }

        private Transform NextSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                return transform;
            }

            Transform point = _spawnPoints[_nextSpawnPointIndex % _spawnPoints.Length];
            _nextSpawnPointIndex++;
            return point != null ? point : transform;
        }

        private void PruneDestroyedActives()
        {
            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                if (_activeInstances[i].IsNullOrDestroyed())
                {
                    _activeInstances.RemoveAt(i);
                }
            }
        }

        private void EnsureTimeService()
        {
            if (_explicitlyInitialized || _time != null)
            {
                return;
            }

            if (GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                GameBootstrapper.Instance.Services.TryGet(out _time);
                GameBootstrapper.Instance.Services.TryGet(out _log);
            }
        }

        private SpawnResult Fail(SpawnFailureReason reason) => SpawnResult.Fail(reason);

        private void LogFailure(string detail)
        {
            _log?.Log(LogLevel.Error, LogCategory, $"Spawn failed on '{name}': {detail}", this);
            if (_log == null)
            {
                Log.Error(LogCategory, $"Spawn failed on '{name}': {detail}", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_configuration == null && _prefab == null)
            {
                Debug.LogWarning($"[Spawner] '{name}' has neither a Configuration nor a Prefab assigned.", this);
            }
        }
#endif
    }
}
