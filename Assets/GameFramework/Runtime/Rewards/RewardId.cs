using System;

namespace GameFramework.Rewards
{
    /// <summary>Stable, designer-authored reward identifier — see <c>CurrencyId</c>'s remarks for
    /// why this wraps a string rather than a runtime-generated id.</summary>
    public readonly struct RewardId : IEquatable<RewardId>
    {
        public readonly string Value;

        public RewardId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(RewardId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RewardId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(RewardId left, RewardId right) => left.Equals(right);
        public static bool operator !=(RewardId left, RewardId right) => !left.Equals(right);
    }
}
