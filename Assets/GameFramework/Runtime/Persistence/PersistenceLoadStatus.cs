namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// Structured outcome of <see cref="IPersistenceService.TryLoad{TData}"/>. Every failure other
    /// than <see cref="Missing"/>/<see cref="Unreadable"/> preserves the raw stored text under
    /// <c>"{key}.corrupt"</c> before returning, so a caller that falls back to defaults (and later
    /// saves over the primary key) never destroys the only copy of the original bytes.
    /// </summary>
    public enum PersistenceLoadStatus
    {
        /// <summary>Stored data was at the current version, passed its integrity check, and
        /// deserialized.</summary>
        Loaded,

        /// <summary>Stored data was at an older version and every step of the registered migration
        /// chain succeeded.</summary>
        Migrated,

        /// <summary>Nothing is stored under the key - the normal "first run" case, not a failure.</summary>
        Missing,

        /// <summary>The envelope or payload could not be parsed, its checksum did not match its
        /// contents (truncation, bit rot, a partial write from an older build, a hand edit), or it
        /// deserialized to null.</summary>
        Corrupted,

        /// <summary>The stored version is newer than the caller's current version (e.g. a save
        /// written by a newer build, then opened by an older one). Rejected rather than
        /// deserialized as if it were current, since fields may have changed meaning.</summary>
        UnsupportedVersion,

        /// <summary>No <see cref="ISaveMigration"/> is registered for some step between the stored
        /// version and the current one - an authoring bug, now distinguishable from "no save
        /// yet".</summary>
        MigrationMissing,

        /// <summary>A registered migration threw, returned null, or did not advance the version
        /// (a chain that would otherwise loop forever).</summary>
        MigrationFailed,

        /// <summary>The storage layer itself failed to read the key (I/O or permission error). The
        /// raw data is not preserved, since it could not be read.</summary>
        Unreadable
    }

    public static class PersistenceLoadStatusExtensions
    {
        /// <summary>True for <see cref="PersistenceLoadStatus.Loaded"/>/<see cref="PersistenceLoadStatus.Migrated"/>
        /// - the only two outcomes that produced real stored data.</summary>
        public static bool IsSuccess(this PersistenceLoadStatus status) =>
            status == PersistenceLoadStatus.Loaded || status == PersistenceLoadStatus.Migrated;

        /// <summary>True for every outcome where stored data existed but could not be used - i.e.
        /// neither success nor <see cref="PersistenceLoadStatus.Missing"/>.</summary>
        public static bool IsFailure(this PersistenceLoadStatus status) =>
            !status.IsSuccess() && status != PersistenceLoadStatus.Missing;
    }
}
