using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Entitlements;
using GameFramework.Monetization.Providers;
using GameFramework.Monetization.Providers.Mock;
using GameFramework.Monetization.Purchases;
using GameFramework.Rewards;
using GameFramework.Runtime.Security;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Monetization
{
    /// <summary>
    /// Drop-in <see cref="GameFramework.Runtime.Bootstrap.GameBootstrapper"/> that additionally
    /// registers Phase 15's three services (<see cref="IEntitlementService"/>,
    /// <see cref="IAdsService"/>, <see cref="IPurchaseService"/>). Extends
    /// <see cref="ProgressionBootstrapper"/> (rather than being a flat sibling) for the same reason
    /// <see cref="GameFramework.Quests.QuestsBootstrapper"/> does: this assembly has a genuine
    /// compile-time dependency on <see cref="GameFramework.Rewards"/> (a purchased consumable claims
    /// through <see cref="IRewardService"/> - see CLAUDE.md's Phase 15 brief, section 24).
    ///
    /// Registers <see cref="IEntitlementService"/> before <see cref="IAdsService"/>/
    /// <see cref="IPurchaseService"/> - both resolve it softly (<c>registry.TryGet</c>) during their
    /// own <see cref="GameFramework.Runtime.Services.IGameService.Initialize"/>, which only succeeds
    /// once it is already registered and initialized (see <see cref="GameBootstrapper.RegisterServices"/>'s
    /// remarks on registration order being load-bearing).
    ///
    /// Registers <see cref="MockAdProvider"/>/<see cref="MockPurchaseProvider"/> while
    /// <c>_useMockProviders</c> is on (the default, preserving Phase 15's behavior in the Editor) -
    /// see CLAUDE.md's Phase 15 brief, section 29/30: no ad or IAP SDK is installed in this project,
    /// so no real provider adapter exists to select instead. Phase 19: the selection goes through
    /// <see cref="DevelopmentProviderGuard"/>, so a non-development player build <b>never</b> gets the
    /// mocks even with the toggle left on - it gets <see cref="NoOpAdProvider"/>/
    /// <see cref="NoOpPurchaseProvider"/> (every purchase/ad fails cleanly) and an error is logged. The
    /// always-succeed mock purchase provider in a release build would otherwise grant paid content for
    /// free. A game shipping with real
    /// monetization replaces these two constructor arguments with its own <see cref="IAdProvider"/>/
    /// <see cref="IPurchaseProvider"/> implementation once a provider SDK is installed - nothing else
    /// in this bootstrapper, or in <see cref="AdsService"/>/<see cref="PurchaseService"/>, changes.
    /// </summary>
    public class MonetizationBootstrapper : ProgressionBootstrapper
    {
        [Header("Monetization Content")]
        [SerializeField] private AdConfiguration _adConfiguration;
        [SerializeField] private ProductCatalog _productCatalog;

        [Header("Mock Providers (Editor/development builds only - see class remarks)")]
        [Tooltip("Ignored in non-development builds, which always use the NoOp providers.")]
        [SerializeField] private bool _useMockProviders = true;
        [SerializeField] private MockAdSimulationMode _mockAdSimulationMode = MockAdSimulationMode.AlwaysSucceed;
        [SerializeField] private MockPurchaseSimulationMode _mockPurchaseSimulationMode = MockPurchaseSimulationMode.AlwaysSucceed;

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IEntitlementService>(new EntitlementService());

            IAdProvider adProvider = DevelopmentProviderGuard.Select<IAdProvider>(
                _useMockProviders, () => new MockAdProvider(_mockAdSimulationMode), () => new NoOpAdProvider(), nameof(MonetizationBootstrapper));
            IPurchaseProvider purchaseProvider = DevelopmentProviderGuard.Select<IPurchaseProvider>(
                _useMockProviders, () => new MockPurchaseProvider(_mockPurchaseSimulationMode), () => new NoOpPurchaseProvider(), nameof(MonetizationBootstrapper));

            registry.Register<IAdsService>(new AdsService(_adConfiguration, adProvider));
            registry.Register<IPurchaseService>(new PurchaseService(_productCatalog, purchaseProvider));
        }
    }
}
