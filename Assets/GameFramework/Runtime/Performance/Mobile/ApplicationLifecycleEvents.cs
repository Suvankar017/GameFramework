namespace GameFramework.Performance.Mobile
{
    /// <summary>Published by <see cref="ApplicationLifecycleService"/> through the Phase 2 Event
    /// System - no separate notification mechanism is introduced for this.</summary>
    public readonly struct ApplicationPausedEvent
    {
    }

    public readonly struct ApplicationResumedEvent
    {
    }

    public readonly struct ApplicationFocusChangedEvent
    {
        public readonly bool HasFocus;
        public ApplicationFocusChangedEvent(bool hasFocus) => HasFocus = hasFocus;
    }

    public readonly struct ApplicationQuittingEvent
    {
    }
}
