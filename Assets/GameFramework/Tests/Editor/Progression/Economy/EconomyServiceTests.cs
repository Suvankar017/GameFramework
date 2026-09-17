using GameFramework.Progression.Economy;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Progression.Tests.Economy
{
    public class EconomyServiceTests
    {
        private CurrencyDefinition _coins;
        private ServiceRegistry _registry;
        private EventService _events;
        private PersistenceService _persistence;
        private EconomyService _economy;

        private static CurrencyId Coins => new CurrencyId("Coins");
        private static CurrencyId Unknown => new CurrencyId("Gems");

        [SetUp]
        public void SetUp()
        {
            _coins = TestDefinitions.Currency("Coins", maxBalance: 1000);
            _registry = TestRegistryFactory.Build(out _, out _events, out _persistence);

            _economy = new EconomyService(new[] { _coins });
            _economy.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_coins != null)
            {
                Object.DestroyImmediate(_coins);
            }
        }

        [Test]
        public void InitialBalance_IsZero()
        {
            Assert.AreEqual(0, _economy.GetBalance(Coins));
        }

        [Test]
        public void TryAdd_ValidAmount_IncreasesBalance()
        {
            AddCurrencyResult result = _economy.TryAdd(Coins, 100);

            Assert.AreEqual(AddCurrencyResult.Success, result);
            Assert.AreEqual(100, _economy.GetBalance(Coins));
        }

        [Test]
        public void TryAdd_PublishesCurrencyChangedEvent()
        {
            CurrencyChangedEvent? received = null;
            _events.Subscribe<CurrencyChangedEvent>(e => received = e);

            _economy.TryAdd(Coins, 50, "Test");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(0, received.Value.PreviousBalance);
            Assert.AreEqual(50, received.Value.NewBalance);
            Assert.AreEqual(50, received.Value.Delta);
            Assert.AreEqual("Test", received.Value.Reason);
        }

        [Test]
        public void TryAdd_NegativeAmount_RejectedAndBalanceUnchanged()
        {
            AddCurrencyResult result = _economy.TryAdd(Coins, -100);

            Assert.AreEqual(AddCurrencyResult.InvalidAmount, result);
            Assert.AreEqual(0, _economy.GetBalance(Coins));
        }

        [Test]
        public void TryAdd_ZeroAmount_Rejected()
        {
            Assert.AreEqual(AddCurrencyResult.InvalidAmount, _economy.TryAdd(Coins, 0));
        }

        [Test]
        public void TryAdd_UnknownCurrency_Rejected()
        {
            Assert.AreEqual(AddCurrencyResult.InvalidCurrency, _economy.TryAdd(Unknown, 100));
        }

        [Test]
        public void TryAdd_BeyondMaxBalance_ClampsAndReportsClamped()
        {
            AddCurrencyResult result = _economy.TryAdd(Coins, 5000);

            Assert.AreEqual(AddCurrencyResult.ClampedToMax, result);
            Assert.AreEqual(1000, _economy.GetBalance(Coins));
        }

        [Test]
        public void TrySpend_SufficientFunds_DecreasesBalance()
        {
            _economy.TryAdd(Coins, 200);

            SpendResult result = _economy.TrySpend(Coins, 150);

            Assert.AreEqual(SpendResult.Success, result);
            Assert.AreEqual(50, _economy.GetBalance(Coins));
        }

        [Test]
        public void TrySpend_InsufficientFunds_RejectedAndBalanceUnchanged()
        {
            _economy.TryAdd(Coins, 50);

            SpendResult result = _economy.TrySpend(Coins, 100);

            Assert.AreEqual(SpendResult.InsufficientFunds, result);
            Assert.AreEqual(50, _economy.GetBalance(Coins));
        }

        [Test]
        public void TrySpend_NegativeAmount_Rejected()
        {
            Assert.AreEqual(SpendResult.InvalidAmount, _economy.TrySpend(Coins, -50));
        }

        [Test]
        public void CanAfford_ReflectsCurrentBalance()
        {
            _economy.TryAdd(Coins, 100);

            Assert.IsTrue(_economy.CanAfford(Coins, 100));
            Assert.IsFalse(_economy.CanAfford(Coins, 101));
        }

        [Test]
        public void MultipleCurrencies_AreIndependent()
        {
            CurrencyDefinition gems = TestDefinitions.Currency("Gems");
            var economy = new EconomyService(new[] { _coins, gems });
            economy.Initialize(TestRegistryFactory.Build(out _, out _, out _));

            economy.TryAdd(Coins, 10);
            economy.TryAdd(new CurrencyId("Gems"), 5);

            Assert.AreEqual(10, economy.GetBalance(Coins));
            Assert.AreEqual(5, economy.GetBalance(new CurrencyId("Gems")));

            Object.DestroyImmediate(gems);
        }

        [Test]
        public void SaveThenLoad_RestoresBalance()
        {
            _economy.TryAdd(Coins, 300);
            _economy.Save();

            var reloaded = new EconomyService(new[] { _coins });
            reloaded.Initialize(_registry);

            Assert.AreEqual(300, reloaded.GetBalance(Coins));
        }

        [Test]
        public void ResetToDefaults_ZeroesBalances()
        {
            _economy.TryAdd(Coins, 100);

            _economy.ResetToDefaults();

            Assert.AreEqual(0, _economy.GetBalance(Coins));
        }

        [Test]
        public void GetRecentTransactions_RecordsAppliedChanges()
        {
            _economy.TryAdd(Coins, 100, "Grant");
            _economy.TrySpend(Coins, 40, "Purchase");

            var transactions = _economy.GetRecentTransactions();

            Assert.AreEqual(2, transactions.Count);
            Assert.AreEqual(100, transactions[0].Delta);
            Assert.AreEqual(-40, transactions[1].Delta);
        }
    }
}
