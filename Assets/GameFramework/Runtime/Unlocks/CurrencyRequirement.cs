using GameFramework.Core.Validation;
using GameFramework.Progression.Economy;

namespace GameFramework.Unlocks
{
    /// <summary>Checks a currency balance — does not spend it. Combine with the game's own
    /// <see cref="IEconomyService.TrySpend"/> call at the point of unlocking if the unlock should
    /// consume the currency.</summary>
    public sealed class CurrencyRequirement : IUnlockRequirement
    {
        private readonly IEconomyService _economy;
        private readonly CurrencyId _currency;
        private readonly int _minAmount;

        public CurrencyRequirement(IEconomyService economy, CurrencyId currency, int minAmount)
        {
            _economy = Guard.NotNull(economy, nameof(economy));
            _currency = currency;
            _minAmount = minAmount;
        }

        public bool IsSatisfied() => _economy.CanAfford(_currency, _minAmount);

        public string Describe() => $"Have {_minAmount} {_currency}";
    }
}
