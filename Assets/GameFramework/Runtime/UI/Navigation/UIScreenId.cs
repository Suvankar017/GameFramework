using System;

namespace GameFramework.UI.Navigation
{
    /// <summary>
    /// Stable, designer-authored screen identifier - the same string-backed-id shape as
    /// <c>GameFlow.LevelId</c>/<c>Presentation.FeedbackId</c>. Deliberately not
    /// <see cref="UnityEngine.Object.GetInstanceID"/>, a <see cref="UnityEngine.GameObject"/> name,
    /// or a position in the navigation stack (CLAUDE.md's Phase 12 brief, section 6).
    /// </summary>
    public readonly struct UIScreenId : IEquatable<UIScreenId>
    {
        public static readonly UIScreenId None = default;

        public readonly string Value;

        public UIScreenId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(UIScreenId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is UIScreenId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(UIScreenId left, UIScreenId right) => left.Equals(right);
        public static bool operator !=(UIScreenId left, UIScreenId right) => !left.Equals(right);
    }
}
