using System;
using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Game-facing in-app purchase API - see CLAUDE.md's Phase 15 brief, section 14. Game code
    /// depends only on this interface, never a store SDK (section 31).
    ///
    /// Unlike Ads (see <c>Ads.IAdsService</c>'s remarks on why it never grants a reward itself), a
    /// completed/restored purchase automatically grants its configured
    /// <see cref="ProductDefinition.GrantedEntitlementId"/>/<see cref="ProductDefinition.GrantedRewardId"/>
    /// - see CLAUDE.md's Phase 15 brief, section 24: a product's grant is fixed, authored data, not
    /// a gameplay-time decision, so there is no seam for the game to intercept it. Every grant is
    /// idempotent per provider transaction id (section 41).
    /// </summary>
    public interface IPurchaseService : IGameService
    {
        MonetizationProviderState State { get; }

        IReadOnlyList<Product> GetProducts();

        bool TryGetProduct(ProductId id, out Product product);

        /// <summary>
        /// Begins a purchase. <paramref name="onComplete"/> is invoked at least once - immediately
        /// and synchronously for a rejection this service can determine without the provider
        /// (<see cref="PurchaseResultKind.ProductUnavailable"/>/<see cref="PurchaseResultKind.NotInitialized"/>/
        /// <see cref="PurchaseResultKind.AlreadyOwned"/>), otherwise once the provider reports an
        /// outcome - and potentially again later for a <see cref="PurchaseResultKind.Pending"/>/
        /// <see cref="PurchaseResultKind.Deferred"/> result once it resolves.
        /// </summary>
        void Purchase(ProductId id, Action<PurchaseResult> onComplete);

        void RestorePurchases(Action<RestoreResult> onComplete);

        /// <summary>True if a transaction id has already been granted - the same check
        /// <see cref="Purchase"/>'s internal idempotency guard uses, exposed for diagnostics/tests.</summary>
        bool IsTransactionProcessed(string transactionId);

        PurchaseDiagnostics GetDiagnostics();

        void Save();
        void Load();

        event Action<PurchaseResult> PurchaseCompleted;
        event Action<PurchaseResult> PurchaseFailed;
        event Action<RestoreResult> RestoreCompleted;
    }
}
