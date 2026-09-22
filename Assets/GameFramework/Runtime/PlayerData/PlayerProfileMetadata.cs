using System;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Persisted metadata for one <see cref="PlayerProfile"/> - deliberately small, per CLAUDE.md's
    /// Phase 13 brief, section 5 ("do not add unnecessary metadata"). Timestamps are stored as
    /// round-trip ("O" format) UTC strings rather than a culture-dependent format or a raw
    /// <see cref="DateTime"/> field (which <c>JsonUtility</c> does not serialize reliably) - see
    /// section 28.
    /// </summary>
    [Serializable]
    internal sealed class PlayerProfileMetadata
    {
        /// <summary>Duplicated from the storage key's own profile id, purely so a loaded metadata
        /// blob can be checked against the key it was loaded from - a cheap integrity check against
        /// a misplaced/corrupted file (see <see cref="PlayerProfileService"/>'s remarks).</summary>
        public string ProfileId;

        public string CreatedAtUtc;
        public string LastModifiedAtUtc;
        public string LastPlayedAtUtc;
        public int SchemaVersion;

        public static PlayerProfileMetadata CreateNew(ProfileId id, int schemaVersion)
        {
            string now = DateTime.UtcNow.ToString("O");
            return new PlayerProfileMetadata
            {
                ProfileId = id.Value,
                CreatedAtUtc = now,
                LastModifiedAtUtc = now,
                LastPlayedAtUtc = now,
                SchemaVersion = schemaVersion
            };
        }
    }
}
