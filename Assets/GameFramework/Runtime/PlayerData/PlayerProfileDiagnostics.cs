using System.Collections.Generic;

namespace GameFramework.PlayerData
{
    /// <summary>
    /// Development-time snapshot of <see cref="PlayerProfileService"/>'s current state - see
    /// CLAUDE.md's Phase 13 brief, section 56. Computed on demand by
    /// <see cref="IPlayerProfileService.GetDiagnostics"/>, not cached, so it is always accurate but
    /// should not be polled every frame in a release build.
    /// </summary>
    public readonly struct PlayerProfileDiagnostics
    {
        public readonly ProfileState State;
        public readonly ProfileId ActiveProfileId;
        public readonly bool HasActiveProfile;
        public readonly bool IsDirty;
        public readonly ProfileOperationResult LastLoadResult;
        public readonly ProfileOperationResult LastSaveResult;
        public readonly bool AutosavePending;
        public readonly IReadOnlyList<string> RegisteredSectionIds;

        public PlayerProfileDiagnostics(
            ProfileState state,
            ProfileId activeProfileId,
            bool hasActiveProfile,
            bool isDirty,
            ProfileOperationResult lastLoadResult,
            ProfileOperationResult lastSaveResult,
            bool autosavePending,
            IReadOnlyList<string> registeredSectionIds)
        {
            State = state;
            ActiveProfileId = activeProfileId;
            HasActiveProfile = hasActiveProfile;
            IsDirty = isDirty;
            LastLoadResult = lastLoadResult;
            LastSaveResult = lastSaveResult;
            AutosavePending = autosavePending;
            RegisteredSectionIds = registeredSectionIds;
        }
    }
}
