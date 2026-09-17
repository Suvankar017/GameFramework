using System;

namespace GameFramework.Progression.Inventory
{
    /// <summary>Stable, designer-authored item identifier — see <c>CurrencyId</c>'s remarks for why
    /// this wraps a string rather than a runtime-generated id.</summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public readonly string Value;

        public ItemId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(ItemId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ItemId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(ItemId left, ItemId right) => left.Equals(right);
        public static bool operator !=(ItemId left, ItemId right) => !left.Equals(right);
    }
}
