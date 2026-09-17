namespace GameFramework.Progression.Experience
{
    /// <summary>
    /// XP-per-level formula, kept out of <see cref="ExperienceService"/> so the runtime manager
    /// never hard-codes a specific curve shape. Integer-only by design — see the framework's
    /// performance/coding policy on avoiding floating-point math where integers suffice.
    /// </summary>
    public interface IProgressionCurve
    {
        /// <summary>XP required to advance from <paramref name="level"/> to <paramref name="level"/> + 1.
        /// Returns 0 (or negative) to mean "no further level exists beyond this one" — the level
        /// this curve considers maximum.</summary>
        int GetRequiredExperience(int level);
    }
}
