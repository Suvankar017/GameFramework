namespace GameFramework.PlayerData
{
    /// <summary>How one piece of a profile was recovered during the last load - see
    /// <see cref="IPlayerProfileService.LastLoadRecoveries"/>.</summary>
    public enum SectionRecoveryKind
    {
        /// <summary>The primary data was unusable; the <c>.bak</c> companion was valid and was
        /// loaded (and written back over the primary).</summary>
        RestoredFromBackup,

        /// <summary>Neither the primary data nor its backup was usable; the section was reset to its
        /// defaults. The unusable bytes are preserved under <c>.corrupt</c> by the persistence layer.</summary>
        ResetToDefaults
    }

    /// <summary>
    /// Structured record of one recovery performed while loading a profile - the machine-readable
    /// counterpart to <see cref="ProfileOperationResult.Reason"/>'s free-form text. Only sections that
    /// actually needed recovery are listed; a clean load produces an empty list. Technical diagnostics
    /// for the game to log/report - whether (and how) a player is told is a game UI decision.
    /// </summary>
    public readonly struct SectionRecovery
    {
        /// <summary>Id used for the profile's own metadata record rather than a registered section.</summary>
        public const string MetadataSectionId = "<metadata>";

        public readonly string SectionId;
        public readonly SectionRecoveryKind Kind;

        public SectionRecovery(string sectionId, SectionRecoveryKind kind)
        {
            SectionId = sectionId;
            Kind = kind;
        }

        public override string ToString() => $"{SectionId} ({Kind})";
    }
}
