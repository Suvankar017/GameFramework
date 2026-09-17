using System;

namespace GameFramework.Progression.Economy
{
    /// <summary>
    /// Stable, designer-authored currency identifier — a thin wrapper around the string a
    /// <see cref="CurrencyDefinition"/> is authored with, not a runtime-generated id. Strings are
    /// the right shape here specifically because they must survive save data and content changes
    /// across sessions, unlike a process-lifetime id such as <c>EntityId</c>.
    /// </summary>
    public readonly struct CurrencyId : IEquatable<CurrencyId>
    {
        public readonly string Value;

        public CurrencyId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(CurrencyId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CurrencyId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(CurrencyId left, CurrencyId right) => left.Equals(right);
        public static bool operator !=(CurrencyId left, CurrencyId right) => !left.Equals(right);
    }
}
