using System.Collections;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.Platform.Tests
{
    /// <summary>
    /// PlayMode is required because <see cref="GameBootstrapper.Awake"/> calls
    /// <c>DontDestroyOnLoad</c> (only legal in Play Mode), and because several Phase 14 services
    /// (<see cref="ScreenService"/>, <see cref="PermissionService"/>) create their own
    /// <c>DontDestroyOnLoad</c> driver GameObjects - see <c>PlayerDataBootstrapperPlayModeTests</c>
    /// for the equivalent Phase 13 coverage this mirrors. <see cref="IDeviceInfoService"/> and
    /// <see cref="IAppStoreService"/> resolve <see cref="IPlatformService"/> through
    /// <c>IServiceRegistry.Get</c> during their own <c>Initialize</c>, which throws unless the
    /// registry has already marked that dependency initialized (an internal, bootstrap-only
    /// mechanism) - so exercising them at all requires going through a real bootstrapper rather than
    /// a hand-built <c>ServiceRegistry</c>.
    /// </summary>
    public class PlatformBootstrapperPlayModeTests
    {
        private GameObject _root;
        private PlatformBootstrapper _bootstrapper;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PlatformBootstrap");
            _bootstrapper = _root.AddComponent<PlatformBootstrapper>();
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
        public void Awake_RegistersAllPlatformServices()
        {
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IPlatformService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IDeviceInfoService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IScreenService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IClipboardService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IPlatformUrlService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IAppStoreService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<INetworkReachabilityService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IPermissionService>());
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IAppSettingsService>());
        }

        [Test]
        public void Awake_StillRegistersPhase1And2Services()
        {
            Assert.DoesNotThrow(() => _bootstrapper.Services.Get<IPersistenceService>());
        }

        [Test]
        public void PlatformService_InEditor_ReportsEditorIdentity()
        {
            var platform = _bootstrapper.Services.Get<IPlatformService>();

            Assert.AreEqual(PlatformType.Editor, platform.Platform);
            Assert.IsTrue(platform.IsEditor);
            Assert.IsFalse(platform.IsMobile);
            Assert.IsFalse(platform.IsDesktop);
        }

        [Test]
        public void DeviceInfoService_Current_HasSaneValues()
        {
            var deviceInfo = _bootstrapper.Services.Get<IDeviceInfoService>();

            PlatformDeviceInfo info = deviceInfo.Current;
            Assert.Greater(info.ProcessorCount, 0);
            Assert.Greater(info.ScreenWidth, 0);
            Assert.Greater(info.ScreenHeight, 0);
            Assert.AreEqual(PlatformType.Editor, info.Platform);
        }

        [Test]
        public void DeviceInfoService_Supports_DoesNotThrowForAnyCapability()
        {
            var deviceInfo = _bootstrapper.Services.Get<IDeviceInfoService>();

            foreach (DeviceCapability capability in System.Enum.GetValues(typeof(DeviceCapability)))
            {
                Assert.DoesNotThrow(() => deviceInfo.Supports(capability));
            }

            Assert.IsTrue(deviceInfo.Supports(DeviceCapability.Clipboard));
        }

        [Test]
        public void ScreenService_ExposesLiveValuesWithoutThrowing()
        {
            var screen = _bootstrapper.Services.Get<IScreenService>();

            Assert.GreaterOrEqual(screen.SafeArea.width, 0f);
            Assert.DoesNotThrow(() => screen.SetOrientation(screen.Orientation));
        }

        [Test]
        public void AppStoreService_WithoutConfiguration_ReportsUnsupported()
        {
            var appStore = _bootstrapper.Services.Get<IAppStoreService>();

            Assert.IsFalse(appStore.HasConfiguration);
            Assert.IsFalse(appStore.OpenStorePage());
            Assert.IsFalse(appStore.OpenReviewPage());
        }

        [Test]
        public void AppSettingsService_InEditor_ReportsUnsupported()
        {
            var appSettings = _bootstrapper.Services.Get<IAppSettingsService>();

            Assert.IsFalse(appSettings.OpenApplicationSettings());
        }

        [UnityTest]
        public IEnumerator PermissionService_RequestPermission_InvokesCallbackExactlyOnce()
        {
            var permissions = _bootstrapper.Services.Get<IPermissionService>();

            int invocationCount = 0;
            PermissionStatus? result = null;
            permissions.RequestPermission(PlatformPermission.Microphone, status =>
            {
                invocationCount++;
                result = status;
            });

            // The Editor typically resolves this synchronously (already authorized), but the async
            // native-prompt path is exercised too if it doesn't - wait a few frames either way.
            for (int i = 0; i < 5 && result == null; i++)
            {
                yield return null;
            }

            Assert.AreEqual(1, invocationCount);
            Assert.IsNotNull(result);
        }

        [Test]
        public void Shutdown_ClearsPlatformServices()
        {
            _bootstrapper.Shutdown();

            Assert.Throws<ServiceNotFoundException>(() => _bootstrapper.Services.Get<IPlatformService>());
        }
    }
}
