using System.Collections;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Persistence;
using GameFramework.Runtime.Settings;
using GameFramework.Runtime.Time;
using GameFramework.Runtime.Timers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.Runtime.Tests.Bootstrap
{
    // Verifies Phase 2 services are wired into the same GameBootstrapper lifecycle Phase 1 tests
    // exercise, and that GameBootstrapper.Update() actually drives ITimerService — the one piece
    // of this integration that can't be checked without a real Unity Update loop.
    public class GameBootstrapperPhase2PlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void Awake_RegistersAndInitializesPhase2Services()
        {
            _root = new GameObject("Bootstrap");
            var bootstrapper = _root.AddComponent<GameBootstrapper>();

            Assert.DoesNotThrow(() => bootstrapper.Services.Get<ITimeService>());
            Assert.DoesNotThrow(() => bootstrapper.Services.Get<ITimerService>());
            Assert.DoesNotThrow(() => bootstrapper.Services.Get<IEventService>());
            Assert.DoesNotThrow(() => bootstrapper.Services.Get<IPersistenceService>());
            Assert.DoesNotThrow(() => bootstrapper.Services.Get<ISettingsService>());
        }

        [UnityTest]
        public IEnumerator Update_TicksTimerService_SoAStartedTimerEventuallyFires()
        {
            _root = new GameObject("Bootstrap");
            var bootstrapper = _root.AddComponent<GameBootstrapper>();
            ITimerService timers = bootstrapper.Services.Get<ITimerService>();

            bool fired = false;
            timers.StartOneShot(0.01f, () => fired = true, TimerTimeMode.Unscaled);

            float waited = 0f;
            while (!fired && waited < 2f)
            {
                yield return null;
                waited += UnityEngine.Time.unscaledDeltaTime;
            }

            Assert.IsTrue(fired);
        }

        [Test]
        public void Shutdown_ShutsDownPhase2ServicesToo()
        {
            _root = new GameObject("Bootstrap");
            var bootstrapper = _root.AddComponent<GameBootstrapper>();

            bootstrapper.Shutdown();

            Assert.Throws<Services.ServiceNotFoundException>(() => bootstrapper.Services.Get<ITimerService>());
        }
    }
}
