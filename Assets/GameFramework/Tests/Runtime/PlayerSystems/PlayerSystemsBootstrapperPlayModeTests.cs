using GameFramework.Audio;
using GameFramework.Feedback;
using GameFramework.Input;
using GameFramework.Localization;
using GameFramework.Runtime.Bootstrap;
using GameFramework.UI;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.PlayerSystems.Tests
{
    /// <summary>
    /// PlayMode is required because <see cref="GameBootstrapper.Awake"/> calls
    /// <c>DontDestroyOnLoad</c> (only legal in Play Mode) — see <c>GameBootstrapperPlayModeTests</c>
    /// for the equivalent Phase 1/2 coverage this extends.
    /// </summary>
    public class PlayerSystemsBootstrapperPlayModeTests
    {
        private GameObject _root;
        private PlayerSystemsBootstrapper _bootstrapper;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PlayerSystemsBootstrap");
            _bootstrapper = _root.AddComponent<PlayerSystemsBootstrapper>();
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
        public void Awake_RegistersAllFivePlayerSystemServices()
        {
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IInputService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<ILocalizationService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IAudioService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IUIService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IFeedbackService>());
        }

        [Test]
        public void Awake_StillRegistersPhase1And2Services()
        {
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<Runtime.Settings.ISettingsService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<Runtime.Persistence.IPersistenceService>());
        }

        [Test]
        public void Shutdown_ClearsPlayerSystemServices()
        {
            _bootstrapper.Shutdown();

            Assert.Throws<Runtime.Services.ServiceNotFoundException>(() => _bootstrapper.Services.Get<IUIService>());
        }
    }
}
