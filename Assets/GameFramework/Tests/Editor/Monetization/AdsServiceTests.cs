using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Entitlements;
using GameFramework.Performance.Mobile;
using GameFramework.Rewards;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Monetization.Tests
{
    public class AdsServiceTests
    {
        private static readonly AdPlacementId Banner = new AdPlacementId("MainBanner");
        private static readonly AdPlacementId Interstitial = new AdPlacementId("LevelInterstitial");
        private static readonly AdPlacementId Rewarded = new AdPlacementId("RewardedRevive");

        private ServiceRegistry _registry;
        private FakeTimeService _time;
        private EventService _events;
        private TimerService _timer;
        private EntitlementService _entitlements;
        private AdConfiguration _config;
        private FakeAdProvider _provider;
        private AdsService _ads;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out _time, out _events, out PersistenceService _, out _timer, out RewardService _, out _entitlements);

            _config = TestDefinitions.AdConfig(new[]
            {
                TestDefinitions.AdPlacement(Banner.Value, AdType.Banner),
                TestDefinitions.AdPlacement(Interstitial.Value, AdType.Interstitial, cooldownSeconds: 5f, sessionShowLimit: 2),
                TestDefinitions.AdPlacement(Rewarded.Value, AdType.Rewarded)
            });

            _provider = new FakeAdProvider();
            _ads = new AdsService(_config, _provider);
            _ads.Initialize(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            _ads.Shutdown();
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_SetsStateInitialized()
        {
            Assert.AreEqual(MonetizationProviderState.Initialized, _ads.State);
        }

        [Test]
        public void CanShow_UnknownPlacement_ReturnsUnknownPlacement()
        {
            Assert.AreEqual(AdAvailabilityReason.UnknownPlacement, _ads.CanShow(new AdPlacementId("nope")));
        }

        [Test]
        public void CanShow_NotLoaded_ReturnsNotLoaded()
        {
            Assert.AreEqual(AdAvailabilityReason.NotLoaded, _ads.CanShow(Interstitial));
        }

        [Test]
        public void Load_ForwardsToProvider_AndBecomesAvailableOnLoaded()
        {
            _ads.Load(Interstitial);
            Assert.Contains(Interstitial, _provider.LoadCalls);

            _provider.RaiseLoaded(Interstitial);
            Assert.IsTrue(_ads.IsAdAvailable(Interstitial));
        }

        [Test]
        public void Show_NotLoaded_ReturnsNotAvailable()
        {
            Assert.AreEqual(AdShowResult.NotAvailable, _ads.Show(Interstitial));
        }

        [Test]
        public void Show_Loaded_ReturnsShown_AndForwardsToProvider()
        {
            _provider.RaiseLoaded(Interstitial);

            AdShowResult result = _ads.Show(Interstitial);

            Assert.AreEqual(AdShowResult.Shown, result);
            Assert.Contains(Interstitial, _provider.ShowCalls);
        }

        [Test]
        public void Show_WhileFullscreenAlreadyShowing_ReturnsAlreadyShowing()
        {
            _provider.RaiseLoaded(Interstitial);
            _ads.Show(Interstitial);
            _provider.RaiseShown(Interstitial);

            Assert.AreEqual(AdShowResult.AlreadyShowing, _ads.Show(Interstitial));
        }

        [Test]
        public void Close_ClearsFullscreenSlot_AllowingAnotherShow()
        {
            _provider.RaiseLoaded(Interstitial);
            _ads.Show(Interstitial);
            _provider.RaiseShown(Interstitial);
            _provider.RaiseClosed(Interstitial);

            _time.Realtime += 5f; // past this placement's cooldown
            _provider.RaiseLoaded(Interstitial);
            Assert.AreEqual(AdShowResult.Shown, _ads.Show(Interstitial));
        }

        [Test]
        public void Cooldown_BlocksShowUntilElapsed()
        {
            _provider.RaiseLoaded(Interstitial);
            _ads.Show(Interstitial);
            _provider.RaiseShown(Interstitial);
            _provider.RaiseClosed(Interstitial);

            Assert.AreEqual(AdAvailabilityReason.Cooldown, _ads.CanShow(Interstitial));

            _time.Realtime += 5f;
            Assert.AreEqual(AdAvailabilityReason.Available, _ads.CanShow(Interstitial));
        }

        [Test]
        public void SessionShowLimit_BlocksAfterLimitReached()
        {
            for (int i = 0; i < 2; i++)
            {
                _provider.RaiseLoaded(Interstitial);
                _ads.Show(Interstitial);
                _provider.RaiseShown(Interstitial);
                _provider.RaiseClosed(Interstitial);
                _time.Realtime += 100f;
            }

            _provider.RaiseLoaded(Interstitial);
            Assert.AreEqual(AdAvailabilityReason.SessionLimitReached, _ads.CanShow(Interstitial));
        }

        [Test]
        public void EntitlementSuppression_BlocksConfiguredTypes_ButNotOthers()
        {
            var entitlementId = new EntitlementId("remove_ads");
            var suppressed = TestDefinitions.AdConfig(
                new[]
                {
                    TestDefinitions.AdPlacement(Banner.Value, AdType.Banner),
                    TestDefinitions.AdPlacement(Rewarded.Value, AdType.Rewarded)
                },
                new[] { TestDefinitions.SuppressionRule("remove_ads", AdType.Banner) });

            var provider = new FakeAdProvider();
            var ads = new AdsService(suppressed, provider);
            ads.Initialize(_registry);

            provider.RaiseLoaded(Banner);
            provider.RaiseLoaded(Rewarded);

            _entitlements.GrantEntitlement(entitlementId, EntitlementSource.Purchase);

            Assert.AreEqual(AdAvailabilityReason.SuppressedByEntitlement, ads.CanShow(Banner));
            Assert.AreEqual(AdAvailabilityReason.Available, ads.CanShow(Rewarded), "Rewarded must not be suppressed unless explicitly configured.");

            ads.Shutdown();
            Object.DestroyImmediate(suppressed);
        }

        [Test]
        public void ShowRewarded_RewardEarned_InvokesCallbackWithRewardEarned()
        {
            _provider.RaiseLoaded(Rewarded);
            RewardedAdResult? result = null;

            bool eventPublished = false;
            _events.Subscribe<AdRewardEarnedEvent>(_ => eventPublished = true);

            _ads.ShowRewarded(Rewarded, r => result = r);
            _provider.RaiseShown(Rewarded);
            _provider.RaiseRewardEarned(Rewarded);
            _provider.RaiseClosed(Rewarded);

            Assert.AreEqual(RewardedAdResult.RewardEarned, result);
            Assert.IsTrue(eventPublished);
        }

        [Test]
        public void ShowRewarded_ClosedWithoutReward_InvokesCallbackWithClosedWithoutReward()
        {
            _provider.RaiseLoaded(Rewarded);
            RewardedAdResult? result = null;

            _ads.ShowRewarded(Rewarded, r => result = r);
            _provider.RaiseShown(Rewarded);
            _provider.RaiseClosed(Rewarded);

            Assert.AreEqual(RewardedAdResult.ClosedWithoutReward, result);
        }

        [Test]
        public void ShowRewarded_ShowFailed_InvokesCallbackWithFailed()
        {
            _provider.RaiseLoaded(Rewarded);
            RewardedAdResult? result = null;

            _ads.ShowRewarded(Rewarded, r => result = r);
            _provider.RaiseShowFailed(Rewarded, "boom");

            Assert.AreEqual(RewardedAdResult.Failed, result);
        }

        [Test]
        public void ShowRewarded_NotLoaded_InvokesCallbackImmediately()
        {
            RewardedAdResult? result = null;
            _ads.ShowRewarded(Rewarded, r => result = r);

            Assert.AreEqual(RewardedAdResult.NotAvailable, result);
            Assert.IsEmpty(_provider.ShowCalls);
        }

        [Test]
        public void ShowRewarded_WhileFullscreenShowing_ReturnsAlreadyShowing()
        {
            _provider.RaiseLoaded(Interstitial);
            _ads.Show(Interstitial);
            _provider.RaiseShown(Interstitial);

            _provider.RaiseLoaded(Rewarded);
            RewardedAdResult? result = null;
            _ads.ShowRewarded(Rewarded, r => result = r);

            Assert.AreEqual(RewardedAdResult.AlreadyShowing, result);
        }

        [Test]
        public void Hide_Banner_ForwardsToProviderAndClearsActiveBanner()
        {
            _provider.RaiseLoaded(Banner);
            _ads.Show(Banner);
            _provider.RaiseShown(Banner);

            _ads.Hide(Banner);

            Assert.Contains(Banner, _provider.HideCalls);
            Assert.IsEmpty(_ads.GetDiagnostics().ActiveBanners);
        }

        [Test]
        public void ApplicationPaused_HidesActiveBanners()
        {
            _provider.RaiseLoaded(Banner);
            _ads.Show(Banner);
            _provider.RaiseShown(Banner);

            _events.Publish(new ApplicationPausedEvent());

            Assert.Contains(Banner, _provider.HideCalls);
        }

        [Test]
        public void ApplicationResumed_ReShowsActiveBanners()
        {
            _provider.RaiseLoaded(Banner);
            _ads.Show(Banner);
            _provider.RaiseShown(Banner);
            _provider.ShowCalls.Clear();

            _events.Publish(new ApplicationPausedEvent());
            _events.Publish(new ApplicationResumedEvent());

            Assert.Contains(Banner, _provider.ShowCalls);
        }

        [Test]
        public void LoadFailed_RetriesUpToMaxRetries()
        {
            _ads.Load(Interstitial);
            _provider.LoadCalls.Clear();

            _provider.RaiseLoadFailed(Interstitial, "fail-1");
            _timer.Tick(); // 0 seconds elapsed yet
            _time.ScaledDeltaTime = 1f;
            _timer.Tick();

            Assert.AreEqual(1, _provider.LoadCalls.Count, "First retry should have fired.");

            _provider.RaiseLoadFailed(Interstitial, "fail-2");
            _time.ScaledDeltaTime = 2f;
            _timer.Tick();

            Assert.AreEqual(2, _provider.LoadCalls.Count, "Second retry (MaxLoadRetries=2) should have fired.");

            _provider.RaiseLoadFailed(Interstitial, "fail-3");
            _time.ScaledDeltaTime = 10f;
            _timer.Tick();

            Assert.AreEqual(2, _provider.LoadCalls.Count, "No further retry once MaxLoadRetries is exhausted.");
        }
    }
}
