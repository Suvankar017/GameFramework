namespace GameFramework.Presentation
{
    /// <summary>
    /// The seven built-in presentation channels a <see cref="FeedbackDefinition"/> can configure.
    /// Distinct from <see cref="Audio.AudioCategory"/> (which governs audio *volume* buses) - a
    /// channel governs whether this orchestration layer attempts that kind of effect at all, via
    /// <see cref="PresentationService"/>'s per-channel settings (see CLAUDE.md's Phase 10 brief,
    /// section 21).
    /// </summary>
    public enum FeedbackChannel
    {
        Audio,
        Haptics,
        Camera,
        Visual,
        Screen,
        UI,
        Time
    }
}
