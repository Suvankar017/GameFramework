namespace GameFramework.Progression.Experience
{
    /// <summary>Published once per individual level crossed — a grant that jumps from level 4 to 7
    /// publishes three of these (4→5, 5→6, 6→7), never only the final transition, so a listener
    /// that cares about every intermediate level (e.g. per-level unlock checks) never misses one.</summary>
    public readonly struct LevelChangedEvent
    {
        public readonly int PreviousLevel;
        public readonly int NewLevel;

        public LevelChangedEvent(int previousLevel, int newLevel)
        {
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
        }
    }
}
