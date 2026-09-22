using System;
using System.Collections.Generic;
using GameFramework.Monetization.Purchases;

namespace GameFramework.Monetization.Tests
{
    /// <summary>Fully controllable <see cref="IPurchaseProvider"/> test double - see
    /// <c>FakeAdProvider</c>'s remarks for why <c>Purchases.PurchaseService</c>'s own orchestration
    /// tests use this instead of <see cref="Providers.Mock.MockPurchaseProvider"/>.</summary>
    internal sealed class FakePurchaseProvider : IPurchaseProvider
    {
        private readonly Dictionary<string, Product> _products = new Dictionary<string, Product>(StringComparer.Ordinal);

        public bool IsInitialized { get; private set; }
        public bool InitializeSucceeds { get; set; } = true;

        public List<ProductId> PurchaseCalls { get; } = new List<ProductId>();
        public Queue<PurchaseResult> NextPurchaseResults { get; } = new Queue<PurchaseResult>();
        public RestoreResult NextRestoreResult { get; set; } = new RestoreResult(true, Array.Empty<ProductId>(), null);

        public event Action<PurchaseResult> PurchaseUpdated;

        public void Initialize(IReadOnlyList<ProductDefinition> products, Action<bool> onComplete)
        {
            foreach (ProductDefinition definition in products)
            {
                _products[definition.Id.Value] = new Product(
                    definition.Id, definition.Type, definition.FallbackDisplayName, string.Empty, "$0.99", "USD", 0.99m, true);
            }

            IsInitialized = true;
            onComplete?.Invoke(InitializeSucceeds);
        }

        public bool TryGetProduct(ProductId id, out Product product) => _products.TryGetValue(id.Value ?? string.Empty, out product);

        public IReadOnlyList<Product> GetProducts()
        {
            var list = new List<Product>();
            list.AddRange(_products.Values);
            return list;
        }

        public void Purchase(ProductId id, Action<PurchaseResult> onComplete)
        {
            PurchaseCalls.Add(id);
            PurchaseResult result = NextPurchaseResults.Count > 0
                ? NextPurchaseResults.Dequeue()
                : new PurchaseResult(PurchaseResultKind.Success, id, "fake-txn-" + PurchaseCalls.Count, null);
            onComplete?.Invoke(result);
        }

        public void RestorePurchases(Action<RestoreResult> onComplete) => onComplete?.Invoke(NextRestoreResult);

        public void RaisePurchaseUpdated(PurchaseResult result) => PurchaseUpdated?.Invoke(result);
    }
}
