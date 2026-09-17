using GameFramework.Quests.Conditions;
using GameFramework.Runtime.Services;

namespace GameFramework.Quests.Achievements
{
    /// <summary>
    /// Generic long-term achievement tracking. The framework defines no concrete achievements - a
    /// game registers an <see cref="AchievementDefinition"/> with the condition that must be
    /// satisfied to complete it. Unlike quests, an achievement has no "not started" state - it begins
    /// tracking progress the moment it is registered.
    /// </summary>
    public interface IAchievementService : IGameService
    {
        /// <summary>Registers an achievement definition and its completion condition. Call once per
        /// achievement at composition-root time.</summary>
        void RegisterAchievement(AchievementDefinition definition, ICondition condition);

        bool IsRegistered(AchievementId id);

        bool IsCompleted(AchievementId id);

        /// <summary>True only once the achievement's reward (if any) has been claimed — derives from
        /// <see cref="GameFramework.Rewards.IRewardService.HasClaimed"/>, never tracked separately.</summary>
        bool IsClaimed(AchievementId id);

        AchievementProgress GetProgress(AchievementId id);

        /// <summary>Claims the achievement's reward via <see cref="GameFramework.Rewards.IRewardService"/>.
        /// Idempotent - a second call after a successful claim always returns
        /// <see cref="AchievementClaimResult.AlreadyClaimed"/>. Not needed for an achievement whose
        /// <see cref="AchievementDefinition.AutoClaimReward"/> is true, but still safe to call.</summary>
        AchievementClaimResult TryClaimReward(AchievementId id);

        void Save();
        void Load();

        /// <summary>Clears every achievement's completion progress (not its content) without touching
        /// saved data on disk until <see cref="Save"/> is called. Development/testing use.</summary>
        void ResetToDefaults();
    }
}
