namespace GameFramework.Performance.Memory
{
    /// <summary>
    /// A point-in-time memory reading from <see cref="MemoryDiagnostics.Sample"/>. Every field here
    /// is either a real measured value or explicitly zero when the platform/build doesn't expose
    /// it - never a guessed or interpolated number. See <see cref="MemoryDiagnostics"/>'s remarks
    /// for exactly what each field means and its limits.
    /// </summary>
    public readonly struct ManagedMemorySample
    {
        /// <summary>Measured via <see cref="System.GC.GetTotalMemory"/> - the managed heap's best
        /// estimate of live managed memory, not total process/native memory.</summary>
        public readonly long ManagedHeapBytes;

        /// <summary>Measured via <see cref="UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong"/>
        /// where available (development builds and the Editor); 0 in a non-development release
        /// build, where that API is not guaranteed to return a meaningful value.</summary>
        public readonly long UnityAllocatedBytes;

        /// <summary>Measured via <see cref="UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong"/>
        /// under the same availability rule as <see cref="UnityAllocatedBytes"/>.</summary>
        public readonly long UnityReservedBytes;

        /// <summary>True if <see cref="UnityAllocatedBytes"/>/<see cref="UnityReservedBytes"/> came
        /// from a real Profiler query (development build or Editor) rather than being unavailable
        /// (both left at 0).</summary>
        public readonly bool HasUnityMemoryData;

        public ManagedMemorySample(long managedHeapBytes, long unityAllocatedBytes, long unityReservedBytes, bool hasUnityMemoryData)
        {
            ManagedHeapBytes = managedHeapBytes;
            UnityAllocatedBytes = unityAllocatedBytes;
            UnityReservedBytes = unityReservedBytes;
            HasUnityMemoryData = hasUnityMemoryData;
        }
    }
}
