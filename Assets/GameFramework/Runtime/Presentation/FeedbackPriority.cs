namespace GameFramework.Presentation
{
    /// <summary>
    /// A <see cref="FeedbackDefinition"/>'s priority - used only to arbitrate the *exclusive*
    /// channels (<see cref="ScreenEffectFeedbackConfig"/>/<see cref="TimeFeedbackConfig"/>; see
    /// <see cref="PresentationService"/>'s remarks on composable vs. exclusive channels). Ordinal
    /// order matters: a lower-priority request never interrupts a higher- or equal-priority one
    /// already playing on an exclusive channel.
    /// </summary>
    public enum FeedbackPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}
