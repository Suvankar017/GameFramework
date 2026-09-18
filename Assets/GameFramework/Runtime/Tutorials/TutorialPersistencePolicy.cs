namespace GameFramework.Tutorials
{
    /// <summary>Governs what <see cref="TutorialService.Save"/> persists for one tutorial - see
    /// CLAUDE.md's Phase 9 brief, section 27. Default is <see cref="CompletionOnly"/>: most
    /// tutorials only need to remember "already shown," not resume mid-sequence.</summary>
    public enum TutorialPersistencePolicy
    {
        /// <summary>Nothing is persisted - every application session starts this tutorial fresh,
        /// regardless of <see cref="TutorialRepeatPolicy"/>.</summary>
        None,

        /// <summary>Only whether the tutorial has completed (and whether that completion was a
        /// skip) is persisted. An interrupted run is not resumed - the next <see cref="ITutorialService.Start"/>
        /// begins at the first step again.</summary>
        CompletionOnly,

        /// <summary>Completion state plus the current step index while running is persisted, so an
        /// application restart mid-tutorial resumes from that step on the next
        /// <see cref="ITutorialService.Start"/> rather than restarting from the first step.</summary>
        ResumeProgress
    }
}
