using System;
using System.Collections.Generic;
using GameFramework.Monetization.Purchases;

namespace GameFramework.Monetization.Providers.Mock
{
    /// <summary>
    /// Deterministic <see cref="IPurchaseProvider"/> for the Editor and automated tests - see
    /// CLAUDE.md's Phase 15 brief, sections 29/39/44. Never grants a real purchase (there is no real
    /// store underneath); synthesizes fake localized catalog data from each
    /// <see cref="ProductDefinition"/>'s fallback display name, and tracks which non-consumable/
    /// subscription products have been "purchased" so a second purchase reports
    /// <c>AlreadyOwned</c>-worthy state is left to <see cref="Purchases.PurchaseService"/> - this
    /// provider only needs to answer <see cref="RestorePurchases"/> honestly for them.
    ///
    /// This is the framework's only shipped <see cref="IPurchaseProvider"/> in this phase - no
    /// Unity IAP adapter exists because that package is not installed in this project (see
    /// CLAUDE.md's Phase 15 brief, section 30).
    /// </summary>
    public sealed class MockPurchaseProvider : IPurchaseProvider
    {
        private readonly MockPurchaseSimulationMode _mode;
        private readonly Dictionary<string, Product> _products = new Dictionary<string, Product>(StringComparer.Ordinal);
        private readonly HashSet<string> _purchasedNonConsumables = new HashSet<string>(StringComparer.Ordinal);
        private int _transactionCounter;

        public bool IsInitialized { get; private set; }

        public event Action<PurchaseResult> PurchaseUpdated;

        public MockPurchaseProvider(MockPurchaseSimulationMode mode = MockPurchaseSimulationMode.AlwaysSucceed)
        {
            _mode = mode;
        }

        public void Initialize(IReadOnlyList<ProductDefinition> products, Action<bool> onComplete)
        {
            _products.Clear();
            foreach (ProductDefinition definition in products)
            {
                _products[definition.Id.Value] = new Product(
                    definition.Id,
                    definition.Type,
                    string.IsNullOrEmpty(definition.FallbackDisplayName) ? definition.Id.Value : definition.FallbackDisplayName,
                    "Mock product for Editor/testing use.",
                    "$0.99",
                    "USD",
                    0.99m,
                    isAvailable: true);
            }

            IsInitialized = true;
            onComplete?.Invoke(true);
        }

        public bool TryGetProduct(ProductId id, out Product product) => _products.TryGetValue(id.Value ?? string.Empty, out product);

        public IReadOnlyList<Product> GetProducts()
        {
            var list = new List<Product>(_products.Count);
            list.AddRange(_products.Values);
            return list;
        }

        public void Purchase(ProductId id, Action<PurchaseResult> onComplete)
        {
            if (!_products.TryGetValue(id.Value ?? string.Empty, out Product product))
            {
                onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.ProductUnavailable, id));
                return;
            }

            switch (_mode)
            {
                case MockPurchaseSimulationMode.AlwaysCancel:
                    onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.Cancelled, id));
                    return;
                case MockPurchaseSimulationMode.AlwaysFail:
                    onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.Failed, id, "Mock: simulated purchase failure."));
                    return;
                case MockPurchaseSimulationMode.AlwaysPending:
                    onComplete?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.Pending, id));
                    return;
            }

            _transactionCounter++;
            string transactionId = $"mock-txn-{_transactionCounter}";

            if (product.Type != ProductType.Consumable)
            {
                _purchasedNonConsumables.Add(id.Value);
            }

            onComplete?.Invoke(new PurchaseResult(PurchaseResultKind.Success, id, transactionId, null));
        }

        public void RestorePurchases(Action<RestoreResult> onComplete)
        {
            var restored = new List<ProductId>(_purchasedNonConsumables.Count);
            foreach (string id in _purchasedNonConsumables)
            {
                restored.Add(new ProductId(id));
            }

            onComplete?.Invoke(new RestoreResult(true, restored, null));
        }

        /// <summary>Test/QA hook - simulates a Pending or Deferred purchase resolving later, via
        /// <see cref="PurchaseUpdated"/> rather than the original <see cref="Purchase"/> callback.</summary>
        internal void CompletePendingPurchase(ProductId id, bool succeeded)
        {
            if (!_products.TryGetValue(id.Value ?? string.Empty, out Product product))
            {
                return;
            }

            if (!succeeded)
            {
                PurchaseUpdated?.Invoke(PurchaseResult.Immediate(PurchaseResultKind.Failed, id, "Mock: deferred purchase declined."));
                return;
            }

            _transactionCounter++;
            string transactionId = $"mock-txn-{_transactionCounter}";

            if (product.Type != ProductType.Consumable)
            {
                _purchasedNonConsumables.Add(id.Value);
            }

            PurchaseUpdated?.Invoke(new PurchaseResult(PurchaseResultKind.Success, id, transactionId, null));
        }
    }
}
