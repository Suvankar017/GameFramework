using System.Collections.Generic;
using GameFramework.Runtime.Services;

namespace GameFramework.Progression.Economy
{
    /// <summary>
    /// Generic multi-currency balance tracking. The framework defines no concrete currencies
    /// ("Coins", "Gems") — a game supplies its own <see cref="CurrencyDefinition"/> assets and
    /// refers to them by <see cref="CurrencyId"/>.
    /// </summary>
    public interface IEconomyService : IGameService
    {
        bool IsRegistered(CurrencyId currency);

        int GetBalance(CurrencyId currency);

        bool CanAfford(CurrencyId currency, int amount);

        /// <summary>Adds <paramref name="amount"/> (must be &gt; 0) to the balance, clamped to
        /// <see cref="CurrencyDefinition.MaxBalance"/> if one is configured. Publishes
        /// <see cref="CurrencyChangedEvent"/> if the balance actually changed.</summary>
        AddCurrencyResult TryAdd(CurrencyId currency, int amount, string reason = null);

        /// <summary>Spends <paramref name="amount"/> (must be &gt; 0) if
        /// <see cref="CanAfford"/> would return true. Publishes <see cref="CurrencyChangedEvent"/>
        /// on success.</summary>
        SpendResult TrySpend(CurrencyId currency, int amount, string reason = null);

        /// <summary>Sets the balance directly (e.g. save-data restoration, debug tooling), clamped
        /// to [MinBalance, MaxBalance]. Publishes <see cref="CurrencyChangedEvent"/> if the balance
        /// actually changed.</summary>
        void SetBalance(CurrencyId currency, int amount, string reason = null);

        /// <summary>Last <paramref name="count"/> transactions across every currency, most recent
        /// last — for debugging/UI/an analytics adapter, not a persisted ledger.</summary>
        IReadOnlyList<EconomyTransaction> GetRecentTransactions();

        void Save();
        void Load();

        /// <summary>Resets every registered currency to its default (0, or
        /// <see cref="CurrencyDefinition.MinBalance"/> if positive) without touching saved data on
        /// disk until <see cref="Save"/> is called. Development/testing use — see the framework's
        /// reset-support conventions.</summary>
        void ResetToDefaults();
    }
}
