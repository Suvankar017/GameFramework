using System;
using System.Globalization;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Public, read-only summary of one profile's metadata - what <see cref="IPlayerProfileService.GetProfileInfo"/>
    /// returns for a save-slot UI the game builds itself (this framework does not build one - see
    /// CLAUDE.md's Phase 13 brief, section 7).
    /// </summary>
    public readonly struct PlayerProfileInfo
    {
        public readonly ProfileId Id;
        public readonly DateTime CreatedAtUtc;
        public readonly DateTime LastModifiedAtUtc;
        public readonly DateTime LastPlayedAtUtc;
        public readonly int SchemaVersion;

        internal PlayerProfileInfo(PlayerProfileMetadata metadata)
        {
            Id = new ProfileId(metadata.ProfileId);
            CreatedAtUtc = ParseUtc(metadata.CreatedAtUtc);
            LastModifiedAtUtc = ParseUtc(metadata.LastModifiedAtUtc);
            LastPlayedAtUtc = ParseUtc(metadata.LastPlayedAtUtc);
            SchemaVersion = metadata.SchemaVersion;
        }

        private static DateTime ParseUtc(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
