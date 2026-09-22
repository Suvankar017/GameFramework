namespace GameFramework.PlayerData
{
    /// <summary>See <see cref="ProfileOperationResult"/>'s remarks - every
    /// <see cref="IPlayerProfileService"/> command returns one of these instead of throwing.</summary>
    public enum ProfileOperationResultKind
    {
        Success,

        /// <summary>Another profile operation (load/save/switch/unload/delete) is already in
        /// progress - see <see cref="IPlayerProfileService.State"/>. Rejected, not queued - see
        /// CLAUDE.md's Phase 13 brief, section 19.</summary>
        AlreadyActive,

        /// <summary>The requested <see cref="ProfileId"/> does not exist.</summary>
        NotFound,

        /// <summary>A profile with the requested <see cref="ProfileId"/> already exists.</summary>
        AlreadyExists,

        /// <summary>The command is not valid from the service's current <see cref="ProfileState"/>
        /// (e.g. <see cref="IPlayerProfileService.Save"/> with no active profile).</summary>
        InvalidState,

        /// <summary>The stored data for one or more sections could not be read even after a
        /// migration attempt and a backup-recovery attempt - see CLAUDE.md's Phase 13 brief,
        /// section 25. The profile is still loaded, with every unrecoverable section left at its
        /// defaults.</summary>
        Corrupted,

        /// <summary>A generic failure with a free-form <see cref="ProfileOperationResult.Reason"/> -
        /// e.g. one or more sections threw from <see cref="IPlayerDataSection.Validate"/> during
        /// save.</summary>
        Failed
    }
}
