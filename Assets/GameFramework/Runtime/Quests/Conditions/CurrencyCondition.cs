using GameFramework.Core.Validation;
using GameFramework.Progression.Economy;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Checks a currency balance (does not spend it) - mirrors
    /// <see cref="GameFramework.Unlocks.CurrencyRequirement"/>, with progress reporting added.</summary>
    public sealed class CurrencyCondition : IProgressCondition
    {
        private readonly IEconomyService _economy;
        private readonly CurrencyId _currency;
        private readonly int _requiredAmount;

        public CurrencyCondition(IEconomyService economy, CurrencyId currency, int requiredAmount)
        {
            _economy = Guard.NotNull(economy, nameof(economy));
            _currency = currency;
            _requiredAmount = requiredAmount;
        }

        public int CurrentValue => _economy.GetBalance(_currency);

        public int RequiredValue => _requiredAmount;

        public bool IsSatisfied() => _economy.CanAfford(_currency, _requiredAmount);

        public string Describe() => $"Have {_requiredAmount} {_currency}";
    }
}
