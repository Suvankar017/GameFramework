using GameFramework.Core.Validation;
using GameFramework.Unlocks;

namespace GameFramework.Rewards
{
    /// <summary>Grants an unlock directly, bypassing its own requirement (via
    /// <see cref="IUnlockService.ForceUnlock"/>) - a reward is itself the "how", so the unlock's
    /// normal requirement (if it has one at all) is irrelevant here. Already being unlocked is not
    /// a failure - a successful no-op.</summary>
    public sealed class UnlockReward : IReward
    {
        private readonly IUnlockService _unlocks;
        private readonly UnlockId _unlockId;

        public UnlockReward(IUnlockService unlocks, UnlockId unlockId)
        {
            _unlocks = Guard.NotNull(unlocks, nameof(unlocks));
            _unlockId = unlockId;
        }

        public bool CanGrant() => _unlocks.IsRegistered(_unlockId);

        public RewardGrantResult Grant()
        {
            UnlockResult result = _unlocks.ForceUnlock(_unlockId, "Reward");
            return result == UnlockResult.InvalidId
                ? RewardGrantResult.Fail($"UnlockReward('{_unlockId}') -> {result}")
                : RewardGrantResult.Ok();
        }
    }
}
