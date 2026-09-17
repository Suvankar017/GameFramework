using GameFramework.Runtime.Services;

namespace GameFramework.Quests.Milestones
{
    /// <summary>
    /// Generic threshold-milestone tracking ("reach N of statistic X"). The framework defines no
    /// concrete milestones - a game registers a <see cref="MilestoneDefinition"/>; the service builds
    /// its own statistic-threshold condition from the definition's data (see
    /// <see cref="MilestoneDefinition"/>'s remarks for why this needs no code-composed condition,
    /// unlike quests/achievements).
    /// </summary>
    public interface IMilestoneService : IGameService
    {
        void RegisterMilestone(MilestoneDefinition definition);

        bool IsRegistered(MilestoneId id);

        bool IsReached(MilestoneId id);

        /// <summary>True only once the milestone's reward (if any) has been claimed — derives from
        /// <see cref="GameFramework.Rewards.IRewardService.HasClaimed"/>, never tracked separately.</summary>
        bool IsClaimed(MilestoneId id);

        MilestoneProgress GetProgress(MilestoneId id);

        /// <summary>Claims the milestone's reward via <see cref="GameFramework.Rewards.IRewardService"/>.
        /// Idempotent - a second call after a successful claim always returns
        /// <see cref="MilestoneClaimResult.AlreadyClaimed"/>.</summary>
        MilestoneClaimResult TryClaimReward(MilestoneId id);

        void Save();
        void Load();

        /// <summary>Clears every milestone's reached state (not its content) without touching saved
        /// data on disk until <see cref="Save"/> is called. Development/testing use.</summary>
        void ResetToDefaults();
    }
}
