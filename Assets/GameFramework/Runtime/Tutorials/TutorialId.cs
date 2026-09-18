using System;

namespace GameFramework.Tutorials
{
    /// <summary>
    /// Stable, designer-authored tutorial identifier - the same string-backed-id shape as
    /// <c>LevelId</c>/<c>QuestId</c>/<c>UnlockId</c>, deliberately not a Unity
    /// <c>GetInstanceID()</c>, a GameObject name, or an array index. Must survive scene changes,
    /// application restarts, and content reordering.
    /// </summary>
    public readonly struct TutorialId : IEquatable<TutorialId>
    {
        public readonly string Value;

        public TutorialId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(TutorialId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TutorialId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(TutorialId left, TutorialId right) => left.Equals(right);
        public static bool operator !=(TutorialId left, TutorialId right) => !left.Equals(right);
    }
}
