using GameFramework.RemoteConfig.Providers;
using GameFramework.RemoteConfig.Providers.Mock;
using NUnit.Framework;

namespace GameFramework.RemoteConfig.Tests
{
    /// <summary>Covers the shipped, game-usable <see cref="MockRemoteConfigProvider"/> - see
    /// <c>Monetization.Tests.MockProviderTests</c>'s remarks for why this is separate from the
    /// orchestration tests that use <see cref="FakeRemoteConfigProvider"/> instead.</summary>
    public class MockProviderTests
    {
        [Test]
        public void AlwaysSucceed_ServesAuthoredEntries()
        {
            MockRemoteConfigEntry entry = TestDefinitions.MockEntry("economy.reward_multiplier", RemoteConfigTypedValue.FromInt(3));
            var provider = new MockRemoteConfigProvider(MockRemoteConfigSimulationMode.AlwaysSucceed, new[] { entry });

            RemoteConfigProviderResult? result = null;
            provider.Fetch(1, r => result = r);

            Assert.IsTrue(result.Value.Success);
            Assert.AreEqual(3, result.Value.Values["economy.reward_multiplier"]);
        }

        [Test]
        public void AlwaysUnavailable_ReportsFailure()
        {
            var provider = new MockRemoteConfigProvider(MockRemoteConfigSimulationMode.AlwaysUnavailable, null);

            RemoteConfigProviderResult? result = null;
            provider.Fetch(1, r => result = r);

            Assert.IsFalse(result.Value.Success);
        }

        [Test]
        public void AlwaysTimeout_NeverInvokesCallback()
        {
            var provider = new MockRemoteConfigProvider(MockRemoteConfigSimulationMode.AlwaysTimeout, null);

            bool invoked = false;
            provider.Fetch(1, _ => invoked = true);

            Assert.IsFalse(invoked);
        }

        [Test]
        public void SchemaMismatch_ReportsNewerSchemaVersion()
        {
            var provider = new MockRemoteConfigProvider(MockRemoteConfigSimulationMode.SchemaMismatch, null);

            RemoteConfigProviderResult? result = null;
            provider.Fetch(1, r => result = r);

            Assert.IsTrue(result.Value.Success);
            Assert.AreEqual(2, result.Value.SchemaVersion);
        }
    }
}
