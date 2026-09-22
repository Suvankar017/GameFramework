using System;
using System.Collections.Generic;
using GameFramework.Analytics.Diagnostics;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Entitlements;
using GameFramework.Monetization.Purchases;
using GameFramework.Runtime.Events;

namespace GameFramework.Analytics.Integration
{
    /// <summary>
    /// Optional Monetization -&gt; Analytics bridge - see CLAUDE.md's Phase 16 brief, sections 22/64.
    /// Consumes Phase 15's own published events only, never Google Mobile Ads/Unity IAP directly, so
    /// swapping the underlying ad/IAP provider never requires touching this bridge (section 71). Only
    /// <c>product_id</c>/<c>result</c> are reported for a purchase - neither the local
    /// <see cref="ProductCatalog"/> nor <see cref="PurchaseResult"/> carries real store price/currency
    /// data, and a transaction id has no analytics use here (it already drives
    /// <c>PurchaseService</c>'s own idempotency) - see CLAUDE.md's Phase 16 brief, section 23. Opt-in,
    /// not bootstrapper-registered - see <see cref="GameFlowAnalyticsIntegration"/>'s remarks for the
    /// pattern.
    /// </summary>
    public sealed class MonetizationAnalyticsIntegration : IDisposable
    {
        private readonly IEventService _events;
        private readonly IAnalyticsService _analytics;
        private readonly IDiagnosticsService _diagnostics;

        public MonetizationAnalyticsIntegration(IEventService events, IAnalyticsService analytics, IDiagnosticsService diagnostics = null)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _diagnostics = diagnostics;

            _events.Subscribe<AdLoadedEvent>(OnAdLoaded);
            _events.Subscribe<AdShownEvent>(OnAdShown);
            _events.Subscribe<AdShowFailedEvent>(OnAdShowFailed);
            _events.Subscribe<AdClosedEvent>(OnAdClosed);
            _events.Subscribe<AdRewardEarnedEvent>(OnAdRewardEarned);
            _events.Subscribe<PurchaseCompletedEvent>(OnPurchaseCompleted);
            _events.Subscribe<PurchaseFailedEvent>(OnPurchaseFailed);
            _events.Subscribe<RestoreCompletedEvent>(OnRestoreCompleted);
            _events.Subscribe<EntitlementChangedEvent>(OnEntitlementChanged);
        }

        public void Dispose()
        {
            _events.Unsubscribe<AdLoadedEvent>(OnAdLoaded);
            _events.Unsubscribe<AdShownEvent>(OnAdShown);
            _events.Unsubscribe<AdShowFailedEvent>(OnAdShowFailed);
            _events.Unsubscribe<AdClosedEvent>(OnAdClosed);
            _events.Unsubscribe<AdRewardEarnedEvent>(OnAdRewardEarned);
            _events.Unsubscribe<PurchaseCompletedEvent>(OnPurchaseCompleted);
            _events.Unsubscribe<PurchaseFailedEvent>(OnPurchaseFailed);
            _events.Unsubscribe<RestoreCompletedEvent>(OnRestoreCompleted);
            _events.Unsubscribe<EntitlementChangedEvent>(OnEntitlementChanged);
        }

        private void OnAdLoaded(AdLoadedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"ad_loaded:{evt.PlacementId}");
            _analytics.Track(EventNames.AdLoaded, new Dictionary<string, object> { ["placement_id"] = evt.PlacementId.Value });
        }

        private void OnAdShown(AdShownEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"ad_shown:{evt.PlacementId}");
            _analytics.Track(EventNames.AdShown, new Dictionary<string, object> { ["placement_id"] = evt.PlacementId.Value });
        }

        private void OnAdShowFailed(AdShowFailedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"ad_show_failed:{evt.PlacementId}");
            _analytics.Track(EventNames.AdFailed, new Dictionary<string, object>
            {
                ["placement_id"] = evt.PlacementId.Value,
                ["reason"] = evt.FailureDetail ?? string.Empty
            });
        }

        private void OnAdClosed(AdClosedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"ad_closed:{evt.PlacementId}");
            _analytics.Track(EventNames.AdClosed, new Dictionary<string, object> { ["placement_id"] = evt.PlacementId.Value });
        }

        private void OnAdRewardEarned(AdRewardEarnedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"ad_reward_earned:{evt.PlacementId}");
            _analytics.Track(EventNames.AdRewardEarned, new Dictionary<string, object> { ["placement_id"] = evt.PlacementId.Value });
        }

        private void OnPurchaseCompleted(PurchaseCompletedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"purchase_completed:{evt.Result.ProductId}");
            _analytics.Track(EventNames.PurchaseCompleted, new Dictionary<string, object>
            {
                ["product_id"] = evt.Result.ProductId.Value,
                ["result"] = evt.Result.Kind.ToString()
            });
        }

        private void OnPurchaseFailed(PurchaseFailedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"purchase_failed:{evt.Result.ProductId}");
            _analytics.Track(EventNames.PurchaseFailed, new Dictionary<string, object>
            {
                ["product_id"] = evt.Result.ProductId.Value,
                ["result"] = evt.Result.Kind.ToString()
            });
        }

        private void OnRestoreCompleted(RestoreCompletedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"restore_completed:{evt.Result.RestoredProductIds.Count}");
            _analytics.Track(EventNames.RestoreCompleted, new Dictionary<string, object>
            {
                ["success"] = evt.Result.Success,
                ["restored_count"] = evt.Result.RestoredProductIds.Count
            });
        }

        private void OnEntitlementChanged(EntitlementChangedEvent evt)
        {
            _diagnostics?.AddBreadcrumb("Monetization", $"entitlement_changed:{evt.Id}");
            _analytics.Track(EventNames.EntitlementChanged, new Dictionary<string, object>
            {
                ["entitlement_id"] = evt.Id.Value,
                ["is_owned"] = evt.IsOwned
            });
        }
    }
}
