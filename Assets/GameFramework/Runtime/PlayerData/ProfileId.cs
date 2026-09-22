using System;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Stable, caller-chosen player-profile identifier - the same string-backed-id shape as
    /// <c>CurrencyId</c>/<c>LevelId</c>/<c>TutorialId</c>, deliberately not a Unity
    /// <c>GetInstanceID()</c>, a save-slot array index, or an object name. Must survive application
    /// restarts, since it is itself part of the persistence key every section is stored under (see
    /// <see cref="PlayerProfileService"/>'s remarks).
    /// </summary>
    public readonly struct ProfileId : IEquatable<ProfileId>
    {
        public readonly string Value;

        public ProfileId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        /// <summary>The profile a game that never exposes profile selection can use exclusively -
        /// see CLAUDE.md's Phase 13 brief, section 35.</summary>
        public static ProfileId Default { get; } = new ProfileId("Default");

        public bool Equals(ProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProfileId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(ProfileId left, ProfileId right) => left.Equals(right);
        public static bool operator !=(ProfileId left, ProfileId right) => !left.Equals(right);
    }
}
