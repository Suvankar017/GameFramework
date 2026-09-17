using GameFramework.Core.Validation;
using GameFramework.Progression.Experience;

namespace GameFramework.Rewards
{
    /// <summary>Grants XP. Already being at max level is not a failure - the grant is simply a
    /// successful no-op, since there is nowhere left for the XP to go.</summary>
    public sealed class ExperienceReward : IReward
    {
        private readonly IExperienceService _experience;
        private readonly int _amount;

        public ExperienceReward(IExperienceService experience, int amount)
        {
            _experience = Guard.NotNull(experience, nameof(experience));
            _amount = amount;
        }

        public bool CanGrant() => _amount > 0;

        public RewardGrantResult Grant()
        {
            AddExperienceResult result = _experience.AddExperience(_amount, "Reward");
            return result == AddExperienceResult.InvalidAmount
                ? RewardGrantResult.Fail($"ExperienceReward({_amount}) -> {result}")
                : RewardGrantResult.Ok();
        }
    }
}
