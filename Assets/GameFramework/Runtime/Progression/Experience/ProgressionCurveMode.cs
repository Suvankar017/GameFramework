namespace GameFramework.Progression.Experience
{
    public enum ProgressionCurveMode
    {
        /// <summary>RequiredXp(level) = BaseExperience + (level - 1) * IncrementPerLevel.</summary>
        Linear,

        /// <summary>RequiredXp(level) = Table[level - 1]; a level beyond the table's length has no
        /// further requirement defined (the table's length is the effective max level).</summary>
        Table
    }
}
