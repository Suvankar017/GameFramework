namespace GameFramework.PlayerData
{
    /// <summary>
    /// <see cref="IPlayerProfileService"/>'s lifecycle state - see CLAUDE.md's Phase 13 brief,
    /// section 18. <see cref="Saving"/> is transient and always returns to <see cref="Loaded"/>
    /// (whether the save succeeded or failed - see <see cref="ProfileOperationResult"/> for the
    /// outcome) rather than a separate stuck "SaveFailed" state, so a failed save never blocks the
    /// profile from being used or saved again; failure is reported through
    /// <see cref="IPlayerProfileService.LastSaveResult"/> and the <c>ProfileSaveFailed</c> event
    /// instead of the state machine.
    /// </summary>
    public enum ProfileState
    {
        /// <summary>No profile is active. The starting state, and the state
        /// <see cref="IPlayerProfileService.UnloadActiveProfile"/> always returns to.</summary>
        Unloaded,

        /// <summary>A profile load is in progress.</summary>
        Loading,

        /// <summary>A profile is active and usable.</summary>
        Loaded,

        /// <summary>A save of the active profile is in progress.</summary>
        Saving,

        /// <summary>The active profile is being unloaded (including as part of
        /// <see cref="IPlayerProfileService.SwitchProfile"/>).</summary>
        Unloading
    }
}
