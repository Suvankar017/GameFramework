using GameFramework.Runtime.Services;

namespace GameFramework.Rewards
{
    /// <summary>
    /// Generic reward claiming/granting, orchestrating Economy/Inventory/Experience/Unlocks rather
    /// than owning any of that state itself. The framework defines no concrete rewards - a game
    /// registers a <see cref="RewardDefinition"/> with the <see cref="IReward"/> content it should
    /// grant.
    /// </summary>
    public interface IRewardService : IGameService
    {
        /// <summary>Registers a reward definition and its content. Call once per reward at
        /// composition-root/content-setup time.</summary>
        void RegisterReward(RewardDefinition definition, IReward content);

        bool IsRegistered(RewardId id);

        /// <summary>True only for a <see cref="RewardClaimPolicy.Once"/> reward that has already
        /// been claimed. Always false for <see cref="RewardClaimPolicy.Repeatable"/> (no claim
        /// state exists for it) and for an unregistered id.</summary>
        bool HasClaimed(RewardId id);

        /// <summary>
        /// Validates and grants a reward's contents. For <see cref="RewardClaimPolicy.Once"/>, a
        /// second call after a successful claim always returns
        /// <see cref="RewardClaimResult.AlreadyClaimed"/> — this is the framework's idempotency
        /// guarantee (see <see cref="RewardClaimResult"/>). Publishes <see cref="RewardGrantedEvent"/>
        /// on success, plus <see cref="RewardClaimedEvent"/> for a <see cref="RewardClaimPolicy.Once"/>
        /// reward.
        /// </summary>
        RewardClaimResult TryClaim(RewardId id, string reason = null);

        void Save();
        void Load();

        /// <summary>Clears every claimed-once record without touching saved data on disk until
        /// <see cref="Save"/> is called. Development/testing use — this does not revoke anything
        /// already granted by Economy/Inventory/Experience/Unlocks, only the claim record.</summary>
        void ResetToDefaults();
    }
}
