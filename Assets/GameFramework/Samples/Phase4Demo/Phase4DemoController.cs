using System.Collections;
using System.Collections.Generic;
using GameFramework.Gameplay;
using GameFramework.Gameplay.Commands;
using GameFramework.Gameplay.Interaction;
using GameFramework.Gameplay.Objectives;
using GameFramework.Gameplay.Pooling;
using GameFramework.Gameplay.Spawning;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using UnityEngine;

namespace GameFramework.Samples.Phase4Demo
{
    /// <summary>
    /// Drives the Phase 4 demonstration scene: exercises the Gameplay Loop, Entities, Object
    /// Lifecycle, Spawning, Pooling, Commands, Interaction/Targeting, and Objectives together, with
    /// no authored assets required (the pooled target "prefab" is built in code). Not part of the
    /// reusable framework - sample/demo content only, kept in its own assembly and scene, separate
    /// from any production game content.
    /// </summary>
    public sealed class Phase4DemoController : MonoBehaviour, IGameplayLifecycle, IGameplayTickable
    {
        private const string LogCategory = "Phase4Demo";
        private const string PoolKey = "Phase4Demo.Target";
        private const int TargetsToSpawn = 5;
        private const float InteractRadius = 6f;

        [Tooltip("Optional - assign a scene Checkpoint to demonstrate reading its data at startup.")]
        [SerializeField] private Checkpoint _checkpoint;

        private readonly List<Phase4DemoTarget> _activeTargets = new List<Phase4DemoTarget>();
        private readonly Collider[] _overlapBuffer = new Collider[8];

        private IGameplayService _gameplay;
        private IEventService _events;
        private Spawner _spawner;
        private GameObject _targetTemplate;
        private Phase4DemoObjective _objective;
        private float _tickAccumulator;
        private bool _ready;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _gameplay = services.Get<IGameplayService>();
            _events = services.Get<IEventService>();
            IPoolService pools = services.Get<IPoolService>();
            ITimerService timers = services.Get<ITimerService>();

            if (_checkpoint != null)
            {
                Log.Info(LogCategory, $"Checkpoint '{_checkpoint.Id}' data read: {_checkpoint.GetData().Position}.");
            }

            _gameplay.RegisterLifecycle(this);
            _gameplay.RegisterTickable(this);
            _gameplay.BeginPlay();

            _targetTemplate = BuildTargetTemplate();
            GameObjectPool pool = pools.GetOrCreate(
                PoolKey, _targetTemplate, new GameObjectPoolConfig { DefaultCapacity = TargetsToSpawn, MaxSize = TargetsToSpawn * 2, PrewarmCount = TargetsToSpawn });

            _spawner = gameObject.AddComponent<Spawner>();
            _spawner.SetProvider(new PooledSpawnProvider(pool));
            _spawner.Spawned += OnTargetSpawned;

            _objective = new Phase4DemoObjective("Phase4Demo.CollectAll", TargetsToSpawn, _events);
            _events.Subscribe<ObjectiveCompletedEvent>(OnObjectiveCompleted);
            _objective.Activate();

            timers.StartRepeating(1f, SpawnOneTarget, repeatCount: TargetsToSpawn, owner: this);

            Phase4DemoLifecycleProbe.RunDemoSequence();

            _ready = true;
            Debug.Log("[Phase4Demo] Ready. Press F to interact with the nearest available target " +
                "(within range of this object), P to toggle pause.");
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                DemoInteractWithNearestTarget();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                DemoTogglePause();
            }
        }

        private void OnDestroy()
        {
            if (_gameplay == null)
            {
                return;
            }

            _gameplay.UnregisterTickable(this);
            _gameplay.UnregisterLifecycle(this);
        }

        private GameObject BuildTargetTemplate()
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "Phase4DemoTarget";
            template.transform.localScale = Vector3.one * 0.75f;
            template.AddComponent<Phase4DemoTarget>();
            template.SetActive(false);
            return template;
        }

        private void SpawnOneTarget()
        {
            Vector3 position = transform.position + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
            _spawner.TrySpawn(new SpawnRequest(_targetTemplate, position, Quaternion.identity));
        }

        private void OnTargetSpawned(GameObject instance)
        {
            var target = instance.GetComponent<Phase4DemoTarget>();
            if (target == null)
            {
                return;
            }

            _activeTargets.Add(target);
            target.Collected += OnTargetCollected;
        }

        private void OnTargetCollected(Phase4DemoTarget target)
        {
            target.Collected -= OnTargetCollected;
            _activeTargets.Remove(target);
            _objective.ReportCollected();
            _spawner.Despawn(target.gameObject);
        }

        private void OnObjectiveCompleted(ObjectiveCompletedEvent e)
        {
            Debug.Log($"[Phase4Demo] Objective '{e.ObjectiveId}' complete - all targets collected!");
        }

        private void DemoInteractWithNearestTarget()
        {
            int count = TargetingUtility.FindTargetsInRadius(transform.position, InteractRadius, ~0, _overlapBuffer);
            Transform closest = TargetingUtility.GetClosest(transform.position, _overlapBuffer, count);
            if (closest == null)
            {
                Debug.Log("[Phase4Demo] No target within range to interact with.");
                return;
            }

            var target = closest.GetComponent<Phase4DemoTarget>();
            if (target == null)
            {
                return;
            }

            var context = new InteractionContext(gameObject, target.gameObject, closest.position, transform.forward);
            CommandResult result = GameplayCommandInvoker.Invoke(new Phase4DemoInteractCommand(target, context));
            Debug.Log($"[Phase4Demo] Interact command result: {result.Status} - {result.Message}");
        }

        private void DemoTogglePause()
        {
            ITimeService time = GameBootstrapper.Instance.Services.Get<ITimeService>();
            if (time.IsPaused)
            {
                time.Resume();
            }
            else
            {
                time.Pause();
            }

            Debug.Log($"[Phase4Demo] Time.IsPaused={time.IsPaused} - the gameplay loop reacts automatically.");
        }

        public void OnGameplayInitialize() => Debug.Log("[Phase4Demo] Gameplay loop: Initialize.");
        public void OnGameplayBeginPlay() => Debug.Log("[Phase4Demo] Gameplay loop: BeginPlay.");
        public void OnGameplayPause() => Debug.Log("[Phase4Demo] Gameplay loop: Pause (tickables stop).");
        public void OnGameplayResume() => Debug.Log("[Phase4Demo] Gameplay loop: Resume (tickables resume).");
        public void OnGameplayShutdown() => Debug.Log("[Phase4Demo] Gameplay loop: Shutdown.");

        public void GameplayTick(float deltaTime)
        {
            _tickAccumulator += deltaTime;
            if (_tickAccumulator < 3f)
            {
                return;
            }

            _tickAccumulator = 0f;
            Debug.Log($"[Phase4Demo] Gameplay heartbeat tick - {_activeTargets.Count} target(s) active, " +
                $"{_objective.CollectedCount}/{TargetsToSpawn} collected.");
        }
    }
}
