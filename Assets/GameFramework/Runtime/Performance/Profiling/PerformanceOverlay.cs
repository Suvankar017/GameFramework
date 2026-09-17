using GameFramework.Runtime.Bootstrap;
using UnityEngine;

namespace GameFramework.Performance.Profiling
{
    /// <summary>
    /// Optional, development-only on-screen readout of <see cref="IPerformanceMonitorService"/>'s
    /// frame stats plus GC memory. Uses <c>OnGUI</c> deliberately - this is a runtime debug HUD, not
    /// custom Editor tooling (which would use UI Toolkit per the framework's Editor tooling
    /// convention), and a single small immediate-mode label is the lightest way to render one
    /// without pulling in <see cref="GameFramework.UI"/>'s canvas/screen machinery for a debug
    /// overlay that has nothing to do with player-facing UI.
    ///
    /// Compiled out of non-development builds entirely via <c>DEVELOPMENT_BUILD</c>/<c>UNITY_EDITOR</c>,
    /// per the framework's development-vs-release split - add this component yourself where you
    /// want it (it is never added automatically by any Bootstrapper).
    /// </summary>
    public sealed class PerformanceOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private bool _visible = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F9;

        private IPerformanceMonitorService _monitor;
        private GUIStyle _style;

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;
            }

            if (_monitor == null && GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready)
            {
                GameBootstrapper.Instance.Services.TryGet(out _monitor);
            }
        }

        private void OnGUI()
        {
            if (!_visible || _monitor == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14 };

            FrameTimeStats stats = _monitor.GetFrameStats();
            long managedMemory = System.GC.GetTotalMemory(false);

            string text =
                $"FPS: {(stats.LastFrameMilliseconds > 0f ? 1000f / stats.LastFrameMilliseconds : 0f):F0}  " +
                $"Frame: {stats.LastFrameMilliseconds:F2}ms (avg {stats.AverageFrameMilliseconds:F2} / worst {stats.WorstFrameMilliseconds:F2})\n" +
                $"Spikes: {stats.SpikeCount}  Budget: {_monitor.FrameBudgetMilliseconds:F2}ms @ {_monitor.TargetFrameRate}fps\n" +
                $"Managed Memory: {managedMemory / (1024f * 1024f):F1} MB (estimated, GC heap only)\n" +
                $"[{_toggleKey}] to toggle";

            GUI.Box(new Rect(10, 10, 420, 90), text, _style);
        }
#endif
    }
}
