using System;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Stable, designer-authored feedback identifier - the same string-backed-id shape as
    /// <c>TutorialId</c>/<c>QuestId</c>/<c>LevelId</c>. Not a Unity <c>GetInstanceID()</c>, a
    /// GameObject name, or an array index.
    /// </summary>
    public readonly struct FeedbackId : IEquatable<FeedbackId>
    {
        public readonly string Value;

        public FeedbackId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(FeedbackId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is FeedbackId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(FeedbackId left, FeedbackId right) => left.Equals(right);
        public static bool operator !=(FeedbackId left, FeedbackId right) => !left.Equals(right);
    }
}
