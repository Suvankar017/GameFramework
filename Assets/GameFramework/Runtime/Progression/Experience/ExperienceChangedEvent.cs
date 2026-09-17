namespace GameFramework.Progression.Experience
{
    /// <summary>Published by <see cref="ExperienceService"/> whenever XP-within-the-current-level
    /// changes — published alongside, not instead of, any <see cref="LevelChangedEvent"/>s the same
    /// <see cref="ExperienceService.AddExperience"/> call also triggers.</summary>
    public readonly struct ExperienceChangedEvent
    {
        public readonly int PreviousExperience;
        public readonly int NewExperience;
        public readonly int Delta;
        public readonly string Reason;

        public ExperienceChangedEvent(int previousExperience, int newExperience, string reason)
        {
            PreviousExperience = previousExperience;
            NewExperience = newExperience;
            Delta = newExperience - previousExperience;
            Reason = reason;
        }
    }
}
