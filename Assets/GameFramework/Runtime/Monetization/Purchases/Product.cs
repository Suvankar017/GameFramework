namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// Provider-independent product info for UI display - see CLAUDE.md's Phase 15 brief, section 15:
    /// game code never sees a provider's own product type (e.g. no <c>UnityEngine.Purchasing.Product</c>).
    /// Populated by <see cref="IPurchaseProvider"/> once its catalog has loaded; before that,
    /// <see cref="IPurchaseService"/> reports <see cref="ProductDefinition.FallbackDisplayName"/>
    /// with <see cref="IsAvailable"/> false.
    /// </summary>
    public readonly struct Product
    {
        public readonly ProductId Id;
        public readonly ProductType Type;
        public readonly string LocalizedTitle;
        public readonly string LocalizedDescription;
        public readonly string LocalizedPriceString;
        public readonly string CurrencyCode;
        public readonly decimal RawPrice;
        public readonly bool IsAvailable;

        public Product(ProductId id, ProductType type, string localizedTitle, string localizedDescription, string localizedPriceString, string currencyCode, decimal rawPrice, bool isAvailable)
        {
            Id = id;
            Type = type;
            LocalizedTitle = localizedTitle;
            LocalizedDescription = localizedDescription;
            LocalizedPriceString = localizedPriceString;
            CurrencyCode = currencyCode;
            RawPrice = rawPrice;
            IsAvailable = isAvailable;
        }

        public static Product Unavailable(ProductId id, ProductType type, string fallbackTitle) =>
            new Product(id, type, fallbackTitle, string.Empty, string.Empty, string.Empty, 0m, false);
    }
}
