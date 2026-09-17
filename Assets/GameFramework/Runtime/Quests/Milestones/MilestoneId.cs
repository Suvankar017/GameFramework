using System;

namespace GameFramework.Quests.Milestones
{
    /// <summary>Stable, designer-authored milestone identifier — see <c>CurrencyId</c>'s remarks for
    /// why this wraps a string rather than a runtime-generated id.</summary>
    public readonly struct MilestoneId : IEquatable<MilestoneId>
    {
        public readonly string Value;

        public MilestoneId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(MilestoneId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MilestoneId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(MilestoneId left, MilestoneId right) => left.Equals(right);
        public static bool operator !=(MilestoneId left, MilestoneId right) => !left.Equals(right);
    }
}
