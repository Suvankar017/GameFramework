using UnityEngine;

namespace GameFramework.Analytics.Diagnostics
{
    /// <summary>Reusable, game-authored diagnostics/crash-reporting configuration - see CLAUDE.md's
    /// Phase 16 brief, section 52.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Analytics/Diagnostics Configuration", fileName = "DiagnosticsConfiguration")]
    public sealed class DiagnosticsConfiguration : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;

        [Tooltip("Oldest breadcrumb is dropped once this many are held.")]
        [SerializeField] private int _breadcrumbCapacity = 50;

        [Tooltip("Hooks UnityEngine.Application.logMessageReceived to capture unhandled exceptions/errors Unity itself logs - see CLAUDE.md's Phase 16 brief, section 29.")]
        [SerializeField] private bool _captureUnhandledExceptions = true;

        public bool Enabled => _enabled;
        public int BreadcrumbCapacity => _breadcrumbCapacity;
        public bool CaptureUnhandledExceptions => _captureUnhandledExceptions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_breadcrumbCapacity < 1) _breadcrumbCapacity = 1;
        }
#endif
    }
}
