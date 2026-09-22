using System;
using System.Collections.Generic;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Provider seam behind <see cref="IPurchaseService"/> - see CLAUDE.md's Phase 15 brief,
    /// sections 3/30. A concrete SDK adapter (e.g. a future Unity IAP provider) implements this and
    /// stays isolated in its own assembly; <see cref="Providers.Mock.MockPurchaseProvider"/> is the
    /// only implementation this phase ships (no IAP SDK is installed in this project).
    ///
    /// Threading note: see <c>Ads.IAdProvider</c>'s remarks - the same main-thread marshaling
    /// responsibility applies here.
    /// </summary>
    public interface IPurchaseProvider
    {
        bool IsInitialized { get; }

        /// <summary>Idempotent. Invokes <paramref name="onComplete"/> exactly once, after the
        /// provider's own product catalog (title/description/localized price) has loaded for every
        /// entry in <paramref name="products"/> it recognizes.</summary>
        void Initialize(IReadOnlyList<ProductDefinition> products, Action<bool> onComplete);

        bool TryGetProduct(ProductId id, out Product product);

        IReadOnlyList<Product> GetProducts();

        /// <summary>Invokes <paramref name="onComplete"/> at least once with the purchase's current
        /// outcome. A <see cref="PurchaseResultKind.Pending"/>/<see cref="PurchaseResultKind.Deferred"/>
        /// result may be followed later by <see cref="PurchaseUpdated"/> reporting the final
        /// outcome for the same product.</summary>
        void Purchase(ProductId id, Action<PurchaseResult> onComplete);

        void RestorePurchases(Action<RestoreResult> onComplete);

        /// <summary>Raised for a purchase outcome that did not arrive through a direct
        /// <see cref="Purchase"/> call's own callback - a deferred/pending transaction resolving
        /// later, or one completed while the app was not running.</summary>
        event Action<PurchaseResult> PurchaseUpdated;
    }
}
