using System;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>Stable, designer-authored logical product key (e.g. "remove_ads", "coins_500") -
    /// see CLAUDE.md's Phase 15 brief, section 16. Never the same as a store's own product id; see
    /// <see cref="ProductDefinition"/> for the logical-to-platform mapping.</summary>
    public readonly struct ProductId : IEquatable<ProductId>
    {
        public readonly string Value;

        public ProductId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(ProductId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProductId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(ProductId left, ProductId right) => left.Equals(right);
        public static bool operator !=(ProductId left, ProductId right) => !left.Equals(right);
    }
}
