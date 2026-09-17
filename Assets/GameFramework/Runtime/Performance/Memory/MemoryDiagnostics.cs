using UnityEngine.Profiling;

namespace GameFramework.Performance.Memory
{
    /// <summary>
    /// Static, dependency-free memory queries - not a registered service, since it has no lifecycle
    /// or dependencies of its own, just point-in-time reads of engine-provided counters. Not a
    /// replacement for the Unity Memory Profiler; use that for anything beyond "roughly how much
    /// managed memory is in use right now".
    ///
    /// <see cref="UnityEngine.Profiling.Profiler"/>'s allocation/reservation queries are only
    /// guaranteed meaningful in the Editor and development builds - a non-development release build
    /// may return 0 for them, which is why <see cref="ManagedMemorySample.HasUnityMemoryData"/>
    /// exists rather than presenting an unreliable 0 as if it were a real reading.
    /// </summary>
    public static class MemoryDiagnostics
    {
        public static ManagedMemorySample Sample()
        {
            long managedHeap = System.GC.GetTotalMemory(forceFullCollection: false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            long allocated = Profiler.GetTotalAllocatedMemoryLong();
            long reserved = Profiler.GetTotalReservedMemoryLong();
            return new ManagedMemorySample(managedHeap, allocated, reserved, hasUnityMemoryData: true);
#else
            return new ManagedMemorySample(managedHeap, 0L, 0L, hasUnityMemoryData: false);
#endif
        }
    }
}
