using GameFramework.Runtime.Services;

namespace GameFramework.Progression.Experience
{
    /// <summary>
    /// Generic player progression — usable for player levels, rank, mastery, or any other single
    /// XP-driven track. The framework defines no concrete RPG-style meaning for "level"; a game
    /// decides what reaching one means.
    /// </summary>
    public interface IExperienceService : IGameService
    {
        int CurrentLevel { get; }

        int CurrentExperience { get; }

        /// <summary>XP still needed to reach <see cref="CurrentLevel"/> + 1, or 0 if already at the
        /// curve's maximum level.</summary>
        int ExperienceToNextLevel { get; }

        bool IsAtMaxLevel { get; }

        /// <summary>
        /// Adds <paramref name="amount"/> (must be &gt; 0) XP, applying every level-up it crosses in
        /// one deterministic pass — a large grant can cross several levels at once. Publishes one
        /// <see cref="ExperienceChangedEvent"/> and one <see cref="LevelChangedEvent"/> per level
        /// actually crossed. A no-op (with <see cref="AddExperienceResult.AtMaxLevel"/>) if already
        /// at the curve's maximum level.
        /// </summary>
        AddExperienceResult AddExperience(int amount, string reason = null);

        /// <summary>Sets level/XP directly (e.g. save-data restoration, debug tooling) without
        /// publishing per-level-crossed events - use <see cref="AddExperience"/> for normal
        /// gameplay grants.</summary>
        void SetLevel(int level, int currentExperience);

        void Save();
        void Load();

        /// <summary>Resets to level 1 with 0 XP without touching saved data on disk until
        /// <see cref="Save"/> is called. Development/testing use.</summary>
        void ResetToDefaults();
    }
}
