namespace GameFramework.Presentation
{
    /// <summary>
    /// Published by <see cref="PresentationService"/> through the existing
    /// <see cref="Runtime.Events.IEventService"/> - no separate notification mechanism.
    /// </summary>
    public readonly struct FeedbackPlayedEvent
    {
        public readonly FeedbackId Id;
        public readonly PlayResult Result;

        public FeedbackPlayedEvent(FeedbackId id, PlayResult result)
        {
            Id = id;
            Result = result;
        }
    }

    /// <summary>
    /// The <see cref="Configs.UIFeedbackConfig"/> channel's actual mechanism - published instead of
    /// this framework calling into <see cref="UI.UIScreen"/>/<see cref="UI.UIPopup"/> directly
    /// (CLAUDE.md's Phase 10 brief, section 17). A game's own UI layer subscribes and decides what,
    /// if anything, to show.
    /// </summary>
    public readonly struct UIFeedbackRequestedEvent
    {
        public readonly FeedbackId Id;

        /// <summary>The triggering <see cref="Configs.UIFeedbackConfig.Tag"/> - free-form, never
        /// interpreted by this framework.</summary>
        public readonly string Tag;

        public UIFeedbackRequestedEvent(FeedbackId id, string tag)
        {
            Id = id;
            Tag = tag;
        }
    }
}
