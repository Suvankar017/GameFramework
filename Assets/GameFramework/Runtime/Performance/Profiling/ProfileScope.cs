using System;
using Unity.Profiling;

namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Reusable Begin/End profiler marker for one <see cref="ProfilingCategory"/>, used as
    /// <c>using (new ProfileScope(ProfilingCategory.Pooling)) { ... }</c>. Wraps Unity's own
    /// <see cref="ProfilerMarker"/> - this is not a replacement for the Unity Profiler, just a
    /// convenient, consistently-categorized way to mark framework boundaries that show up in it.
    ///
    /// Gated by <see cref="PerformanceSettings.Mode"/>: when <see cref="ProfilingMode.Disabled"/>,
    /// construction/disposal is a single branch with no marker Begin/End call - safe to leave
    /// <c>using</c> blocks in hot paths (<see cref="GameFramework.Gameplay.Pooling.GameObjectPool"/>'s
    /// Get/Release, the Phase 5 tick loop) without paying for it in a release build.
    /// </summary>
    public readonly struct ProfileScope : IDisposable
    {
        private static readonly ProfilerMarker[] Markers = BuildMarkers();

        private readonly bool _active;
        private readonly int _categoryIndex;

        public ProfileScope(ProfilingCategory category)
        {
            _categoryIndex = (int)category;
            _active = PerformanceSettings.IsProfilingEnabled;
            if (_active)
            {
                Markers[_categoryIndex].Begin();
            }
        }

        public void Dispose()
        {
            if (_active)
            {
                Markers[_categoryIndex].End();
            }
        }

        private static ProfilerMarker[] BuildMarkers()
        {
            var names = Enum.GetNames(typeof(ProfilingCategory));
            var markers = new ProfilerMarker[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                markers[i] = new ProfilerMarker($"GameFramework.{names[i]}");
            }

            return markers;
        }
    }
}
