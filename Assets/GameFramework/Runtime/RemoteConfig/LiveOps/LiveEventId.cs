using System;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Stable, designer-authored logical event key (e.g. "summer_event") - see CLAUDE.md's
    /// Phase 17 brief, section 47. Never a localized display name.</summary>
    public readonly struct LiveEventId : IEquatable<LiveEventId>
    {
        public readonly string Value;

        public LiveEventId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(LiveEventId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LiveEventId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(LiveEventId left, LiveEventId right) => left.Equals(right);
        public static bool operator !=(LiveEventId left, LiveEventId right) => !left.Equals(right);
    }
}
