using System;

namespace GameFramework.Unlocks
{
    /// <summary>Stable, designer-authored unlock identifier — see <c>CurrencyId</c>'s remarks for
    /// why this wraps a string rather than a runtime-generated id. Identifies any unlockable thing
    /// (item, level, character, feature) generically; the framework attaches no specific meaning to
    /// what unlocking one actually does in-game.</summary>
    public readonly struct UnlockId : IEquatable<UnlockId>
    {
        public readonly string Value;

        public UnlockId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(UnlockId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is UnlockId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(UnlockId left, UnlockId right) => left.Equals(right);
        public static bool operator !=(UnlockId left, UnlockId right) => !left.Equals(right);
    }
}
