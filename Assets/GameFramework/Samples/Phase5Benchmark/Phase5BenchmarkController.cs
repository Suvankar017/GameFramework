using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using GameFramework.Gameplay.Pooling;
using GameFramework.Performance.Memory;
using GameFramework.Performance.Profiling;
using GameFramework.Performance.Ticking;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameFramework.Samples.Phase5Benchmark
{
    /// <summary>
    /// Isolated Phase 5 benchmark/stress-test scene - not a production scene, not added to Build
    /// Settings, exactly like the other phase demos. Every measurement here uses
    /// <see cref="Stopwatch"/> around the real production code path (the actual
    /// <see cref="TickService"/>/<see cref="GameObjectPool"/> a game would use), logged to the
    /// console so the numbers in the framework documentation/final report are real console output
    /// from a run of this scene, not invented values. These are Editor-environment measurements,
    /// not a claim about any specific device's performance - see the framework documentation's
    /// note on what a frame-rate budget is and isn't.
    /// </summary>
    public sealed class Phase5BenchmarkController : MonoBehaviour
    {
        private const string LogCategory = "Phase5Benchmark";
        private const string TickBudgetName = "Phase5Benchmark.TickAll";

        private sealed class BenchmarkTickable : ITickable
        {
            public int TickCount;
            public void Tick(float deltaTime) => TickCount++;
        }

        private readonly List<BenchmarkTickable> _tickables = new List<BenchmarkTickable>();

        private ITickService _tickService;
        private IPerformanceMonitorService _monitor;
        private GameObjectPool _benchmarkPool;
        private GameObject _poolPrefab;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _tickService = services.Get<ITickService>();
            _monitor = services.Get<IPerformanceMonitorService>();
            _monitor.RegisterBudget(TickBudgetName, 2f);

            _poolPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _poolPrefab.name = "Phase5BenchmarkPoolPrefab";
            _poolPrefab.SetActive(false);
            _benchmarkPool = new GameObjectPool(_poolPrefab, new GameObjectPoolConfig { DefaultCapacity = 32, MaxSize = 2000 });

            Debug.Log("[Phase5Benchmark] Ready. Keys: 1/2/3 register 100/500/1000 tickables, " +
                "T = tick benchmark, P = pool Get/Release benchmark (1000 cycles), " +
                "M = memory sample, R = full development report.");
        }

        private void Update()
        {
            if (_tickService == null)
            {
                return; // Start's coroutine hasn't reached Ready yet.
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) RegisterTickables(100);
            if (Input.GetKeyDown(KeyCode.Alpha2)) RegisterTickables(500);
            if (Input.GetKeyDown(KeyCode.Alpha3)) RegisterTickables(1000);
            if (Input.GetKeyDown(KeyCode.T)) RunTickBenchmark();
            if (Input.GetKeyDown(KeyCode.P)) RunPoolBenchmark();
            if (Input.GetKeyDown(KeyCode.M)) LogMemorySample();
            if (Input.GetKeyDown(KeyCode.R)) LogDevelopmentReport();
        }

        private void RegisterTickables(int count)
        {
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < _tickables.Count; i++)
            {
                _tickService.Unregister(_tickables[i]);
            }

            _tickables.Clear();

            for (int i = 0; i < count; i++)
            {
                var tickable = new BenchmarkTickable();
                _tickables.Add(tickable);
                _tickService.Register(tickable);
            }

            stopwatch.Stop();
            Debug.Log($"[Phase5Benchmark] Registered {count} tickables in {stopwatch.Elapsed.TotalMilliseconds:F3}ms " +
                $"(TickableCount now {_tickService.TickableCount}).");
        }

        private void RunTickBenchmark()
        {
            if (_tickables.Count == 0)
            {
                Debug.Log("[Phase5Benchmark] Register tickables first (1/2/3).");
                return;
            }

            const int iterations = 100;
            // ITickService's public contract is Register/Unregister only - Tick() invocation is
            // meant to be driven by GameBootstrapper via IUpdatableService, not called by game
            // code. This benchmark deliberately reaches past that contract to measure the real
            // Tick() cost directly; production code should never do this.
            var updatable = (IUpdatableService)_tickService;
            using (new ProfileScope(ProfilingCategory.Framework))
            {
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    updatable.Tick();
                }

                stopwatch.Stop();

                double totalMs = stopwatch.Elapsed.TotalMilliseconds;
                double avgMs = totalMs / iterations;
                _monitor.ReportSample(TickBudgetName, (float)avgMs);

                Debug.Log($"[Phase5Benchmark] Tick x{iterations} over {_tickService.TickableCount} tickables: " +
                    $"{totalMs:F3}ms total, {avgMs:F4}ms/tick average.");
            }
        }

        private void RunPoolBenchmark()
        {
            const int cycles = 1000;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < cycles; i++)
            {
                GameObject instance = _benchmarkPool.Get();
                _benchmarkPool.Release(instance);
            }

            stopwatch.Stop();

            PoolStatistics stats = _benchmarkPool.Statistics;
            double totalMs = stopwatch.Elapsed.TotalMilliseconds;
            Debug.Log($"[Phase5Benchmark] Pool Get+Release x{cycles}: {totalMs:F3}ms total, " +
                $"{totalMs / cycles:F5}ms/cycle average. Stats: GetCount={stats.GetCount}, " +
                $"ReleaseCount={stats.ReleaseCount}, TotalCreatedCount={stats.TotalCreatedCount}, " +
                $"PeakActiveCount={stats.PeakActiveCount}.");
        }

        private void LogMemorySample()
        {
            ManagedMemorySample sample = MemoryDiagnostics.Sample();
            string unityPart = sample.HasUnityMemoryData
                ? $"Unity allocated {sample.UnityAllocatedBytes / (1024f * 1024f):F1}MB / reserved {sample.UnityReservedBytes / (1024f * 1024f):F1}MB"
                : "Unity memory data unavailable outside a development build";

            Debug.Log($"[Phase5Benchmark] Managed heap: {sample.ManagedHeapBytes / (1024f * 1024f):F1}MB (estimated). {unityPart}.");
        }

        private void LogDevelopmentReport()
        {
            FrameTimeStats frame = _monitor.GetFrameStats();
            PoolStatistics pool = _benchmarkPool.Statistics;
            ManagedMemorySample memory = MemoryDiagnostics.Sample();

            Debug.Log("[Phase5Benchmark] --- Development Performance Report ---\n" +
                $"Frame: last {frame.LastFrameMilliseconds:F2}ms, avg {frame.AverageFrameMilliseconds:F2}ms, " +
                $"worst {frame.WorstFrameMilliseconds:F2}ms, spikes {frame.SpikeCount} (over {frame.SampleCount} samples)\n" +
                $"Tickables: {_tickService.TickableCount} variable, {_tickService.FixedTickableCount} fixed, {_tickService.LateTickableCount} late\n" +
                $"Pool: active {pool.CurrentActiveCount}, inactive {pool.CurrentInactiveCount}, peak {pool.PeakActiveCount}, " +
                $"totalCreated {pool.TotalCreatedCount}, misses {pool.MissCount}\n" +
                $"Managed Memory: {memory.ManagedHeapBytes / (1024f * 1024f):F1}MB (estimated)");
        }
    }
}
