namespace GameFramework.PlayerData
{
    /// <summary>
    /// Lifecycle events published through <see cref="Runtime.Events.IEventService"/> by
    /// <see cref="PlayerProfileService"/> - see CLAUDE.md's Phase 13 brief, section 37. Each has a
    /// matching plain C# event directly on <see cref="IPlayerProfileService"/> for code that already
    /// holds the service, exactly like <c>GameFlow.IGameFlowService.StateChanged</c>/
    /// <see cref="Runtime.Events.IEventService"/> already do side by side.
    ///
    /// Deliberately not included: a per-mutation "ProfileDirty" event - section 37 asks lifecycle
    /// events to "represent meaningful lifecycle boundaries," and a dirty transition can happen many
    /// times a second (every coin pickup); <see cref="IPlayerProfileService.IsDirty"/> is already
    /// queryable on demand for anything that needs it.
    /// </summary>
    public readonly struct ProfileLoadingEvent
    {
        public readonly ProfileId Id;
        public ProfileLoadingEvent(ProfileId id) => Id = id;
    }

    public readonly struct ProfileLoadedEvent
    {
        public readonly ProfileId Id;
        public ProfileLoadedEvent(ProfileId id) => Id = id;
    }

    public readonly struct ProfileLoadFailedEvent
    {
        public readonly ProfileId Id;
        public readonly string Reason;
        public ProfileLoadFailedEvent(ProfileId id, string reason)
        {
            Id = id;
            Reason = reason;
        }
    }

    public readonly struct ProfileSavingEvent
    {
        public readonly ProfileId Id;
        public ProfileSavingEvent(ProfileId id) => Id = id;
    }

    public readonly struct ProfileSavedEvent
    {
        public readonly ProfileId Id;
        public ProfileSavedEvent(ProfileId id) => Id = id;
    }

    public readonly struct ProfileSaveFailedEvent
    {
        public readonly ProfileId Id;
        public readonly string Reason;
        public ProfileSaveFailedEvent(ProfileId id, string reason)
        {
            Id = id;
            Reason = reason;
        }
    }

    public readonly struct ProfileUnloadingEvent
    {
        public readonly ProfileId Id;
        public ProfileUnloadingEvent(ProfileId id) => Id = id;
    }

    public readonly struct ProfileUnloadedEvent
    {
        public readonly ProfileId Id;
        public ProfileUnloadedEvent(ProfileId id) => Id = id;
    }

    /// <summary>Published after the new profile is fully loaded and active - the seam a game's own
    /// UI Navigation composition uses to move to a main menu/gameplay once player data is ready
    /// (see CLAUDE.md's Phase 13 brief, section 39): "Profile Service -&gt; Profile Loaded Event -&gt;
    /// Navigation Service -&gt; Main Menu." This framework never references Navigation directly.</summary>
    public readonly struct ProfileSwitchedEvent
    {
        public readonly ProfileId PreviousId;
        public readonly ProfileId CurrentId;
        public ProfileSwitchedEvent(ProfileId previousId, ProfileId currentId)
        {
            PreviousId = previousId;
            CurrentId = currentId;
        }
    }
}
