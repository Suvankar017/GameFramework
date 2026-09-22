using GameFramework.Runtime.Persistence;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// One modular, independently-versioned slice of a <see cref="PlayerProfile"/> - e.g. a game's
    /// own <c>ProgressionDataSection</c>, <c>InventoryDataSection</c>. See CLAUDE.md's Phase 13
    /// brief, sections 9-11.
    ///
    /// Most games should derive from <see cref="PlayerDataSection{TData}"/> rather than implement
    /// this directly - it already implements <see cref="Save"/>/<see cref="Load"/>/
    /// <see cref="ResetToDefaults"/> against a plain <see cref="Runtime.Persistence.IPersistenceService"/>-
    /// compatible data class, following exactly the Save/Load/dirty-flag shape every existing
    /// persisted framework system (<c>EconomyService</c>, <c>SettingsService</c>,
    /// <c>TutorialService</c>) already hand-writes for itself.
    /// </summary>
    public interface IPlayerDataSection
    {
        /// <summary>Stable id, unique within one <see cref="PlayerProfileService"/> - also the
        /// distinguishing part of this section's persistence key (see
        /// <see cref="PlayerProfileService"/>'s remarks on key construction).</summary>
        string Id { get; }

        /// <summary>This section's own schema version, independent of any other section's - see
        /// CLAUDE.md's Phase 13 brief, section 21.</summary>
        int Version { get; }

        /// <summary>True since the last successful <see cref="Save"/>/<see cref="Load"/> if a
        /// mutation has marked this section dirty. <see cref="PlayerProfileService"/> aggregates
        /// this across every registered section for <see cref="IPlayerProfileService.IsDirty"/>.</summary>
        bool IsDirty { get; }

        /// <summary>Replaces this section's in-memory data with its defaults and marks it dirty.
        /// Called for a newly created profile, and for a section a stored save never referenced
        /// before (e.g. content added in a later build).</summary>
        void ResetToDefaults();

        /// <summary>
        /// Checked after <see cref="ResetToDefaults"/>, after <see cref="Load"/> (post-migration),
        /// and before <see cref="Save"/> - see CLAUDE.md's Phase 13 brief, section 29. Implementations
        /// should repair whatever they safely can in place (clamp an out-of-range value, drop a
        /// dangling reference) rather than throw; throwing should be reserved for state this section
        /// genuinely cannot make sense of, which <see cref="PlayerProfileService"/> treats as
        /// corruption (see <see cref="ProfileOperationResultKind.Corrupted"/>) rather than letting
        /// bad data reach - or overwrite - disk.
        /// </summary>
        void Validate();

        /// <summary>Persists this section's current data under <paramref name="storageKey"/> (already
        /// fully qualified by <see cref="PlayerProfileService"/> - this method does not need to know
        /// about profile scoping) and clears <see cref="IsDirty"/>.</summary>
        void Save(IPersistenceService persistence, string storageKey);

        /// <summary>Loads this section's data from <paramref name="storageKey"/>, migrating and
        /// validating it, and clears <see cref="IsDirty"/>. Leaves defaults in place (from the last
        /// <see cref="ResetToDefaults"/>) if nothing is stored yet under that key.</summary>
        void Load(IPersistenceService persistence, string storageKey);

        /// <summary>Copies whatever is currently stored under <paramref name="storageKey"/> (the
        /// value about to be overwritten) to <paramref name="backupKey"/> - see
        /// <see cref="PlayerProfileService"/>'s remarks on its backup strategy. A no-op if nothing
        /// is currently stored under <paramref name="storageKey"/>.</summary>
        void CreateBackup(IPersistenceService persistence, string storageKey, string backupKey);
    }
}
