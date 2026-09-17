using GameFramework.Core.Validation;
using GameFramework.Progression.Economy;

namespace GameFramework.Rewards
{
    public sealed class CurrencyReward : IReward
    {
        private readonly IEconomyService _economy;
        private readonly CurrencyId _currency;
        private readonly int _amount;

        public CurrencyReward(IEconomyService economy, CurrencyId currency, int amount)
        {
            _economy = Guard.NotNull(economy, nameof(economy));
            _currency = currency;
            _amount = amount;
        }

        public bool CanGrant() => _economy.IsRegistered(_currency) && _amount > 0;

        public RewardGrantResult Grant()
        {
            AddCurrencyResult result = _economy.TryAdd(_currency, _amount, "Reward");
            return result == AddCurrencyResult.InvalidCurrency || result == AddCurrencyResult.InvalidAmount
                ? RewardGrantResult.Fail($"CurrencyReward('{_currency}', {_amount}) -> {result}")
                : RewardGrantResult.Ok();
        }
    }
}
