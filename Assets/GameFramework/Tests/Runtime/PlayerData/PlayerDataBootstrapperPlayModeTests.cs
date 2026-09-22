using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Persistence;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.PlayerData.Tests
{
    /// <summary>
    /// PlayMode is required because <see cref="GameBootstrapper.Awake"/> calls
    /// <c>DontDestroyOnLoad</c> (only legal in Play Mode), and because
    /// <see cref="PlayerProfileService"/> itself creates a <c>DontDestroyOnLoad</c> driver
    /// GameObject - see <c>PlayerSystemsBootstrapperPlayModeTests</c> for the equivalent Phase 3
    /// coverage this mirrors.
    /// </summary>
    public class PlayerDataBootstrapperPlayModeTests
    {
        private GameObject _root;
        private PlayerDataBootstrapper _bootstrapper;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PlayerDataBootstrap");
            _bootstrapper = _root.AddComponent<PlayerDataBootstrapper>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void Awake_ReachesReadyState()
        {
            Assert.AreEqual(BootstrapState.Ready, _bootstrapper.State);
        }

        [Test]
        public void Awake_RegistersPlayerProfileService()
        {
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IPlayerProfileService>());
        }

        [Test]
        public void Awake_StillRegistersPhase1And2Services()
        {
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IPersistenceService>());
        }

        [Test]
        public void RegisterSectionThenLoadDefaultProfile_Works()
        {
            var playerData = (PlayerProfileService)_bootstrapper.Services.Get<IPlayerProfileService>();
            playerData.RegisterSection(() => new TestProgressionSection());

            ProfileOperationResult result = playerData.LoadDefaultProfile();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ProfileId.Default, playerData.ActiveProfile.Id);
        }

        [Test]
        public void ApplicationPauseCallback_FlushesDirtyProfile()
        {
            var playerData = (PlayerProfileService)_bootstrapper.Services.Get<IPlayerProfileService>();
            playerData.RegisterSection(() => new TestProgressionSection());
            playerData.LoadDefaultProfile();
            playerData.ActiveProfile.GetSection<TestProgressionSection>().SetInteger(3);

            // Simulates OnApplicationPause(true) - Unity does not let a test invoke that callback
            // directly, so this calls the same internal handler PlayerDataLifecycleDriver forwards
            // to (see that class's remarks).
            playerData.HandleApplicationPaused();

            Assert.IsFalse(playerData.IsDirty);
        }

        [Test]
        public void Shutdown_ClearsPlayerProfileService()
        {
            _bootstrapper.Shutdown();

            Assert.Throws<Runtime.Services.ServiceNotFoundException>(
                () => _bootstrapper.Services.Get<IPlayerProfileService>());
        }
    }
}
