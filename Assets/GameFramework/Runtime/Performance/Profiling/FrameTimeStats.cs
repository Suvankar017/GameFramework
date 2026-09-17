namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// A point-in-time snapshot of <see cref="PerformanceMonitorService"/>'s frame timing. All
    /// values are measured (real <see cref="UnityEngine.Time.unscaledDeltaTime"/> samples), never
    /// estimated - see <see cref="PerformanceMonitorService"/>'s remarks on what this can and
    /// cannot tell you.
    /// </summary>
    public readonly struct FrameTimeStats
    {
        public readonly float LastFrameMilliseconds;
        public readonly float AverageFrameMilliseconds;
        public readonly float WorstFrameMilliseconds;
        public readonly int SpikeCount;
        public readonly int SampleCount;

        public FrameTimeStats(
            float lastFrameMilliseconds,
            float averageFrameMilliseconds,
            float worstFrameMilliseconds,
            int spikeCount,
            int sampleCount)
        {
            LastFrameMilliseconds = lastFrameMilliseconds;
            AverageFrameMilliseconds = averageFrameMilliseconds;
            WorstFrameMilliseconds = worstFrameMilliseconds;
            SpikeCount = spikeCount;
            SampleCount = sampleCount;
        }
    }
}
