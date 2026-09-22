using GameFramework.Analytics;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Analytics
{
    /// <summary>
    /// Development-time inspection for Phase 16 - the same "log diagnostics for whichever
    /// GameBootstrapper is currently running" pattern as
    /// <c>Monetization.MonetizationDiagnosticsMenu</c>/<c>Platform.PlatformDiagnosticsMenu</c> (a
    /// menu-item log, not a game-facing dashboard - see CLAUDE.md's Phase 16 brief, sections 85/86).
    /// </summary>
    internal static class AnalyticsDiagnosticsMenu
    {
        [MenuItem("GameFramework/Analytics/Log Diagnostics")]
        private static void LogDiagnostics()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                Debug.LogWarning("[Analytics] No ready GameBootstrapper instance found - enter Play Mode first.");
                return;
            }

            IServiceRegistry registry = GameBootstrapper.Instance.Services;

            if (registry.TryGet(out IAnalyticsService analytics))
            {
                AnalyticsDiagnostics diag = analytics.GetDiagnostics();
                Debug.Log($"[Analytics] Enabled={diag.IsEnabled}, Consent={diag.Consent}, Provider={diag.ProviderName}, " +
                    $"Session={diag.SessionId}, UserId={diag.UserId}, EventsSent={diag.EventsSentCount}, " +
                    $"Queued={diag.QueuedEventCount}, LastEvent={diag.LastEventName}");
            }
            else
            {
                Debug.LogWarning("[Analytics] No IAnalyticsService is registered on the running GameBootstrapper.");
            }

            if (registry.TryGet(out IDiagnosticsService diagnostics))
            {
                Debug.Log($"[Analytics] Diagnostics: Enabled={diagnostics.IsEnabled}, Breadcrumbs={diagnostics.GetBreadcrumbs().Count}");
            }
            else
            {
                Debug.LogWarning("[Analytics] No IDiagnosticsService is registered on the running GameBootstrapper.");
            }
        }
    }
}
