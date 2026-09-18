using System;

namespace GameFramework.GameFlow
{
    /// <summary>
    /// Stable, designer-authored level/stage identifier - the same string-backed-id shape as
    /// <c>CurrencyId</c>/<c>ItemId</c>/<c>UnlockId</c>, deliberately not a Build Settings scene
    /// index (which can change as scenes are added/reordered) and not a runtime-generated id.
    /// </summary>
    public readonly struct LevelId : IEquatable<LevelId>
    {
        public readonly string Value;

        public LevelId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(LevelId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LevelId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(LevelId left, LevelId right) => left.Equals(right);
        public static bool operator !=(LevelId left, LevelId right) => !left.Equals(right);
    }
}
