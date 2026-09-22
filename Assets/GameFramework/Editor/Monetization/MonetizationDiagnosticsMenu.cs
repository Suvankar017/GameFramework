using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Entitlements;
using GameFramework.Monetization.Purchases;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Monetization
{
    /// <summary>
    /// Development-time inspection for Phase 15 - see CLAUDE.md's Phase 15 brief, section 38.
    /// Follows the same "log diagnostics for whichever GameBootstrapper is currently running"
    /// pattern as <c>Platform.PlatformDiagnosticsMenu</c>/<c>PlayerData.PlayerDataDiagnosticsMenu</c>
    /// (a menu-item log, not a UI Toolkit window - this phase has no game-facing diagnostics UI to
    /// build, see section 38's "must not become a game-facing UI framework").
    /// </summary>
    internal static class MonetizationDiagnosticsMenu
    {
        [MenuItem("GameFramework/Monetization/Log Diagnostics")]
        private static void LogDiagnostics()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                Debug.LogWarning("[Monetization] No ready GameBootstrapper instance found - enter Play Mode first.");
                return;
            }

            IServiceRegistry registry = GameBootstrapper.Instance.Services;

            if (registry.TryGet(out IAdsService ads))
            {
                AdsDiagnostics adsDiag = ads.GetDiagnostics();
                Debug.Log($"[Monetization] Ads: State={adsDiag.State}, Loaded=[{string.Join(", ", adsDiag.LoadedPlacements)}], " +
                    $"ActiveBanners=[{string.Join(", ", adsDiag.ActiveBanners)}], LastError={adsDiag.LastError}");
            }
            else
            {
                Debug.LogWarning("[Monetization] No IAdsService is registered on the running GameBootstrapper.");
            }

            if (registry.TryGet(out IPurchaseService purchases))
            {
                PurchaseDiagnostics purchaseDiag = purchases.GetDiagnostics();
                Debug.Log($"[Monetization] Purchases: State={purchaseDiag.State}, Products={purchaseDiag.AvailableProducts.Count}, " +
                    $"ProcessedTransactions={purchaseDiag.ProcessedTransactionCount}, LastError={purchaseDiag.LastError}");
            }
            else
            {
                Debug.LogWarning("[Monetization] No IPurchaseService is registered on the running GameBootstrapper.");
            }

            if (registry.TryGet(out IEntitlementService entitlements))
            {
                Debug.Log($"[Monetization] Entitlements owned: [{string.Join(", ", entitlements.OwnedEntitlements)}]");
            }
            else
            {
                Debug.LogWarning("[Monetization] No IEntitlementService is registered on the running GameBootstrapper.");
            }
        }
    }
}
