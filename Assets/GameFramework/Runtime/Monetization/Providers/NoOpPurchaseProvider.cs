using System;
using System.Collections.Generic;
using GameFramework.Monetization.Purchases;

namespace GameFramework.Monetization.Providers
{
    /// <summary>
    /// Production-safe "no IAP SDK installed" provider: initialization reports failure, so
    /// <see cref="PurchaseService"/> rejects every purchase with
    /// <see cref="PurchaseResultKind.NotInitialized"/> and grants nothing. This is what a release build
    /// gets when <see cref="MonetizationBootstrapper"/>'s mock toggle is left on (see
    /// <c>Runtime.Security.DevelopmentProviderGuard</c>) - the always-succeed mock would otherwise
    /// hand out paid entitlements for free.
    /// </summary>
    public sealed class NoOpPurchaseProvider : IPurchaseProvider
    {
        public bool IsInitialized => false;

        public void Initialize(IReadOnlyList<ProductDefinition> products, Action<bool> onComplete) => onComplete?.Invoke(false);

        public bool TryGetProduct(ProductId id, out Product product)
        {
            product = default;
            return false;
        }

        public IReadOnlyList<Product> GetProducts() => Array.Empty<Product>();

        public void Purchase(ProductId id, Action<PurchaseResult> onComplete) =>
            onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.NotAvailable, id, "No purchase provider is installed."));

        public void RestorePurchases(Action<RestoreResult> onComplete) =>
            onComplete?.Invoke(new RestoreResult(false, Array.Empty<ProductId>(), "No purchase provider is installed."));

#pragma warning disable 0067 // Never raised: there is no store to report deferred outcomes.
        public event Action<PurchaseResult> PurchaseUpdated;
#pragma warning restore 0067
    }
}
