using System;

namespace GameFramework.Quests.Quests
{
    /// <summary>Stable, designer-authored quest identifier — see <c>CurrencyId</c>'s remarks for why
    /// this wraps a string rather than a runtime-generated id.</summary>
    public readonly struct QuestId : IEquatable<QuestId>
    {
        public readonly string Value;

        public QuestId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(QuestId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is QuestId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(QuestId left, QuestId right) => left.Equals(right);
        public static bool operator !=(QuestId left, QuestId right) => !left.Equals(right);
    }
}
