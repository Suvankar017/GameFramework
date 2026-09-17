using System;

namespace GameFramework.Progression.Statistics
{
    /// <summary>Stable, designer-authored statistic identifier — see <c>CurrencyId</c>'s remarks for
    /// why this wraps a string rather than a runtime-generated id.</summary>
    public readonly struct StatisticId : IEquatable<StatisticId>
    {
        public readonly string Value;

        public StatisticId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(StatisticId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StatisticId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(StatisticId left, StatisticId right) => left.Equals(right);
        public static bool operator !=(StatisticId left, StatisticId right) => !left.Equals(right);
    }
}
