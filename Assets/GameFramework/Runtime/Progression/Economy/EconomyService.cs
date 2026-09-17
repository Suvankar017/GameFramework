using System;
using System.Collections.Generic;
using GameFramework.Core.Validation;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;

namespace GameFramework.Progression.Economy
{
    /// <summary>
    /// Default <see cref="IEconomyService"/>. Currency definitions are constructor-injected (like
    /// <c>PersistenceService</c>'s storage/serializer) rather than scanned from the project at
    /// runtime — the composition root supplies whatever <see cref="CurrencyDefinition"/> assets the
    /// game authored. Follows the same explicit Save/Load + dirty-flag-on-Shutdown policy as
    /// <c>SettingsService</c>: nothing here writes to disk on every mutation.
    /// </summary>
    public sealed class EconomyService : IEconomyService
    {
        private const string LogCategory = "Economy";
        private const string SaveKey = "GameFramework.Progression.Economy";
        private const int SaveVersion = 1;
        private const int TransactionLogCapacity = 50;

        private readonly Dictionary<CurrencyId, CurrencyDefinition> _definitions = new Dictionary<CurrencyId, CurrencyDefinition>();
        private readonly Dictionary<CurrencyId, int> _balances = new Dictionary<CurrencyId, int>();
        private readonly Queue<EconomyTransaction> _transactions = new Queue<EconomyTransaction>(TransactionLogCapacity + 1);
        private readonly List<EconomyTransaction> _transactionsSnapshot = new List<EconomyTransaction>();

        private IPersistenceService _persistence;
        private IEventService _events;
        private ILoggingService _log;
        private ITimeService _time;
        private int _nextTransactionId;
        private bool _isDirty;

        public EconomyService(IEnumerable<CurrencyDefinition> definitions)
        {
            Guard.NotNull(definitions, nameof(definitions));

            foreach (CurrencyDefinition definition in definitions)
            {
                if (definition == null || !definition.Id.IsValid)
                {
                    continue;
                }

                if (_definitions.ContainsKey(definition.Id))
                {
                    throw new InvalidOperationException($"Duplicate currency id '{definition.Id}'.");
                }

                _definitions.Add(definition.Id, definition);
                _balances.Add(definition.Id, ClampToDefinition(definition, 0));
            }
        }

        public void Initialize(IServiceRegistry registry)
        {
            _persistence = registry.Get<IPersistenceService>();
            _events = registry.Get<IEventService>();
            _time = registry.Get<ITimeService>();
            registry.TryGet(out _log);
            Load();
        }

        public void Shutdown()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        public bool IsRegistered(CurrencyId currency) => _definitions.ContainsKey(currency);

        public int GetBalance(CurrencyId currency) => _balances.TryGetValue(currency, out int balance) ? balance : 0;

        public bool CanAfford(CurrencyId currency, int amount)
        {
            return amount >= 0 && _balances.TryGetValue(currency, out int balance) && balance >= amount;
        }

        public AddCurrencyResult TryAdd(CurrencyId currency, int amount, string reason = null)
        {
            if (!_definitions.TryGetValue(currency, out CurrencyDefinition definition))
            {
                LogRejected("TryAdd", currency, "unregistered currency");
                return AddCurrencyResult.InvalidCurrency;
            }

            if (amount <= 0)
            {
                LogRejected("TryAdd", currency, $"non-positive amount {amount}");
                return AddCurrencyResult.InvalidAmount;
            }

            int previous = _balances[currency];
            long requested = (long)previous + amount; // widen before clamping to avoid int overflow
            int clamped = ClampToDefinition(definition, requested);
            bool wasClamped = clamped != requested;

            ApplyBalance(currency, previous, clamped, reason);
            return wasClamped ? AddCurrencyResult.ClampedToMax : AddCurrencyResult.Success;
        }

        public SpendResult TrySpend(CurrencyId currency, int amount, string reason = null)
        {
            if (!_definitions.TryGetValue(currency, out CurrencyDefinition definition))
            {
                LogRejected("TrySpend", currency, "unregistered currency");
                return SpendResult.InvalidCurrency;
            }

            if (amount <= 0)
            {
                LogRejected("TrySpend", currency, $"non-positive amount {amount}");
                return SpendResult.InvalidAmount;
            }

            int previous = _balances[currency];
            if (previous < amount)
            {
                return SpendResult.InsufficientFunds;
            }

            int newBalance = ClampToDefinition(definition, previous - amount);
            ApplyBalance(currency, previous, newBalance, reason);
            return SpendResult.Success;
        }

        public void SetBalance(CurrencyId currency, int amount, string reason = null)
        {
            if (!_definitions.TryGetValue(currency, out CurrencyDefinition definition))
            {
                LogRejected("SetBalance", currency, "unregistered currency");
                return;
            }

            int previous = _balances[currency];
            int clamped = ClampToDefinition(definition, amount);
            ApplyBalance(currency, previous, clamped, reason);
        }

        public IReadOnlyList<EconomyTransaction> GetRecentTransactions()
        {
            _transactionsSnapshot.Clear();
            _transactionsSnapshot.AddRange(_transactions);
            return _transactionsSnapshot;
        }

        public void Save()
        {
            var data = new EconomySaveData();
            foreach (KeyValuePair<CurrencyId, int> pair in _balances)
            {
                data.CurrencyIds.Add(pair.Key.Value);
                data.Balances.Add(pair.Value);
            }

            _persistence.Save(SaveKey, data, SaveVersion);
            _isDirty = false;
        }

        public void Load()
        {
            EconomySaveData data = _persistence.Load(SaveKey, SaveVersion, new EconomySaveData());

            for (int i = 0; i < data.CurrencyIds.Count && i < data.Balances.Count; i++)
            {
                var currency = new CurrencyId(data.CurrencyIds[i]);
                if (!_definitions.TryGetValue(currency, out CurrencyDefinition definition))
                {
                    continue; // currency no longer exists in this build - ignore, not an error
                }

                // Clamping (not rejecting) handles corrupted/out-of-range saved balances safely.
                _balances[currency] = ClampToDefinition(definition, data.Balances[i]);
            }

            _isDirty = false;
        }

        public void ResetToDefaults()
        {
            foreach (CurrencyId currency in new List<CurrencyId>(_balances.Keys))
            {
                CurrencyDefinition definition = _definitions[currency];
                int previous = _balances[currency];
                ApplyBalance(currency, previous, ClampToDefinition(definition, 0), "Reset");
            }
        }

        private void ApplyBalance(CurrencyId currency, int previous, int newBalance, string reason)
        {
            _balances[currency] = newBalance;
            if (newBalance == previous)
            {
                return;
            }

            _isDirty = true;
            RecordTransaction(currency, newBalance - previous, newBalance, reason);
            _events.Publish(new CurrencyChangedEvent(currency, previous, newBalance, reason));
        }

        private void RecordTransaction(CurrencyId currency, int delta, int balanceAfter, string reason)
        {
            if (_transactions.Count >= TransactionLogCapacity)
            {
                _transactions.Dequeue();
            }

            _nextTransactionId++;
            _transactions.Enqueue(new EconomyTransaction(_nextTransactionId, currency, delta, balanceAfter, reason, _time.Realtime));
        }

        private static int ClampToDefinition(CurrencyDefinition definition, long value)
        {
            int min = definition.MinBalance;
            int max = definition.MaxBalance;

            if (max > 0 && value > max)
            {
                value = max;
            }

            if (value < min)
            {
                value = min;
            }

            return (int)value;
        }

        private void LogRejected(string operation, CurrencyId currency, string detail)
        {
            _log?.Log(LogLevel.Warning, LogCategory, $"{operation}('{currency}') rejected: {detail}.");
        }
    }
}
