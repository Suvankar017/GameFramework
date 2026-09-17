namespace GameFramework.Progression.Experience
{
    public enum AddExperienceResult
    {
        Success,
        InvalidAmount,

        /// <summary>Already at the curve's maximum level - no XP was added.</summary>
        AtMaxLevel
    }
}
