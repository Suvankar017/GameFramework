using System;

namespace GameFramework.Quests.Achievements
{
    /// <summary>Stable, designer-authored achievement identifier — see <c>CurrencyId</c>'s remarks
    /// for why this wraps a string rather than a runtime-generated id.</summary>
    public readonly struct AchievementId : IEquatable<AchievementId>
    {
        public readonly string Value;

        public AchievementId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(AchievementId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AchievementId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(AchievementId left, AchievementId right) => left.Equals(right);
        public static bool operator !=(AchievementId left, AchievementId right) => !left.Equals(right);
    }
}
