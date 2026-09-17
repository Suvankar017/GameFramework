namespace GameFramework.Gameplay.Pooling
{
    /// <summary>
    /// A point-in-time snapshot of one <see cref="GameObjectPool"/>'s usage, added in Phase 5 for
    /// development diagnostics (the performance overlay, a development report) - reading it has no
    /// effect on the pool itself and costs nothing beyond copying a few ints.
    /// </summary>
    public readonly struct PoolStatistics
    {
        public readonly int GetCount;
        public readonly int ReleaseCount;

        /// <summary>Number of times <see cref="GameObjectPool.Get()"/> found a pooled instance that
        /// had been destroyed outside the pool and transparently created a replacement - see
        /// <see cref="GameObjectPool"/>'s remarks on "Safety, not just happy-path".</summary>
        public readonly int MissCount;

        /// <summary>Total instances this pool has ever created (initial prewarm plus every later
        /// growth beyond it).</summary>
        public readonly int TotalCreatedCount;

        public readonly int PeakActiveCount;
        public readonly int CurrentActiveCount;
        public readonly int CurrentInactiveCount;

        public PoolStatistics(
            int getCount,
            int releaseCount,
            int missCount,
            int totalCreatedCount,
            int peakActiveCount,
            int currentActiveCount,
            int currentInactiveCount)
        {
            GetCount = getCount;
            ReleaseCount = releaseCount;
            MissCount = missCount;
            TotalCreatedCount = totalCreatedCount;
            PeakActiveCount = peakActiveCount;
            CurrentActiveCount = currentActiveCount;
            CurrentInactiveCount = currentInactiveCount;
        }
    }
}
