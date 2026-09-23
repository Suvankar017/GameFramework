using GameFramework.RemoteConfig;
using GameFramework.RemoteConfig.FeatureFlags;
using GameFramework.RemoteConfig.LiveOps;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.RemoteConfig
{
    /// <summary>
    /// Development-time inspection for Phase 17 - see CLAUDE.md's Phase 17 brief, section 81. Follows
    /// the same "log diagnostics for whichever GameBootstrapper is currently running" pattern as
    /// <c>Monetization.MonetizationDiagnosticsMenu</c>/<c>Platform.PlatformDiagnosticsMenu</c> (a
    /// menu-item log, not a UI Toolkit window - this phase has no game-facing diagnostics UI to
    /// build).
    /// </summary>
    internal static class RemoteConfigDiagnosticsMenu
    {
        [MenuItem("GameFramework/Remote Config/Log Diagnostics")]
        private static void LogDiagnostics()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                Debug.LogWarning("[RemoteConfig] No ready GameBootstrapper instance found - enter Play Mode first.");
                return;
            }

            IServiceRegistry registry = GameBootstrapper.Instance.Services;

            if (registry.TryGet(out IRemoteConfigService remoteConfig))
            {
                RemoteConfigDiagnostics diag = remoteConfig.GetDiagnostics();
                Debug.Log($"[RemoteConfig] State={diag.State}, Environment={diag.Environment}, Version={diag.Version}, " +
                    $"SchemaVersion={diag.SchemaVersion}, Cache={diag.CacheStatus}, Keys={diag.KeyCount}, " +
                    $"LastFetchUtc={diag.LastFetchUtc}, LastActivationUtc={diag.LastActivationUtc}, LastError={diag.LastError}");
            }
            else
            {
                Debug.LogWarning("[RemoteConfig] No IRemoteConfigService is registered on the running GameBootstrapper.");
            }

            if (registry.TryGet(out ILiveOpsService liveOps))
            {
                LiveOpsDiagnostics diag = liveOps.GetDiagnostics();
                Debug.Log($"[RemoteConfig] LiveOps: TotalEvents={diag.TotalEvents}, Active={diag.ActiveCount}, Upcoming={diag.UpcomingCount}");

                foreach (LiveEventId id in liveOps.GetEventIds())
                {
                    Debug.Log($"[RemoteConfig] LiveEvent '{id}': {liveOps.GetState(id)}");
                }
            }
            else
            {
                Debug.LogWarning("[RemoteConfig] No ILiveOpsService is registered on the running GameBootstrapper.");
            }

            if (!registry.TryGet(out IFeatureFlagService _))
            {
                Debug.LogWarning("[RemoteConfig] No IFeatureFlagService is registered on the running GameBootstrapper.");
            }
        }
    }
}
