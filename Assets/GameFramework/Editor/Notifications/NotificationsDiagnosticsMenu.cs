using GameFramework.DeepLinks;
using GameFramework.Notifications;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Notifications
{
    /// <summary>
    /// Development-time inspection for Phase 18 - see CLAUDE.md's Phase 18 brief, section 30/48.
    /// Follows the same "log diagnostics for whichever GameBootstrapper is currently running" pattern
    /// as <c>RemoteConfig.Editor.RemoteConfigDiagnosticsMenu</c>/<c>Monetization.MonetizationDiagnosticsMenu</c>.
    /// </summary>
    internal static class NotificationsDiagnosticsMenu
    {
        [MenuItem("GameFramework/Notifications/Log Diagnostics")]
        private static void LogDiagnostics()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                Debug.LogWarning("[Notifications] No ready GameBootstrapper instance found - enter Play Mode first.");
                return;
            }

            IServiceRegistry registry = GameBootstrapper.Instance.Services;

            if (registry.TryGet(out INotificationService notifications))
            {
                NotificationDiagnostics diag = notifications.GetDiagnostics();
                Debug.Log($"[Notifications] Supported={diag.IsSupported}, Permission={diag.PermissionStatus}, " +
                    $"Scheduled=[{string.Join(", ", diag.ScheduledIds)}], LastError={diag.LastError}");
            }
            else
            {
                Debug.LogWarning("[Notifications] No INotificationService is registered on the running GameBootstrapper.");
            }

            if (registry.TryGet(out IDeepLinkService deepLinks))
            {
                DeepLinkDiagnostics diag = deepLinks.GetDiagnostics();
                Debug.Log($"[DeepLinks] Ready={diag.IsReady}, Handlers={diag.HandlerCount}, " +
                    $"Pending={diag.PendingRawUri}, LastProcessed={diag.LastProcessedRawUri}");
            }
            else
            {
                Debug.LogWarning("[Notifications] No IDeepLinkService is registered on the running GameBootstrapper.");
            }
        }
    }
}
