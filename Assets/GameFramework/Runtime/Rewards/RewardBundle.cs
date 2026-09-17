using GameFramework.Core.Validation;

namespace GameFramework.Rewards
{
    /// <summary>Composes several rewards into one logical grant (e.g. a level-completion reward of
    /// currency + XP + an item) - itself an <see cref="IReward"/>, so bundles can nest.
    /// <see cref="CanGrant"/> requires every child to agree first; <see cref="Grant"/> stops at the
    /// first real failure it encounters (which <see cref="CanGrant"/> having already passed makes
    /// very unlikely in a single-threaded game - see <see cref="RewardService"/>'s remarks).</summary>
    public sealed class RewardBundle : IReward
    {
        private readonly IReward[] _rewards;

        public RewardBundle(params IReward[] rewards)
        {
            Guard.NotNull(rewards, nameof(rewards));
            _rewards = rewards;
        }

        public bool CanGrant()
        {
            for (int i = 0; i < _rewards.Length; i++)
            {
                if (!_rewards[i].CanGrant())
                {
                    return false;
                }
            }

            return true;
        }

        public RewardGrantResult Grant()
        {
            for (int i = 0; i < _rewards.Length; i++)
            {
                RewardGrantResult result = _rewards[i].Grant();
                if (!result.Success)
                {
                    return result;
                }
            }

            return RewardGrantResult.Ok();
        }
    }
}
