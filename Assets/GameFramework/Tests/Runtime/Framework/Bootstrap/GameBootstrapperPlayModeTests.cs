using System;
using System.Collections;
using GameFramework.Core.Extensions;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.State;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.Runtime.Tests.Bootstrap
{
    // GameBootstrapper.Awake calls DontDestroyOnLoad, which Unity only allows in Play Mode, so
    // this whole class runs as PlayMode tests (see GameFramework.Core's TransformExtensions for
    // the same constraint applied to Object.Destroy).
    public class GameBootstrapperPlayModeTests
    {
        private GameObject _primary;
        private GameObject _secondary;

        [TearDown]
        public void TearDown()
        {
            if (_secondary != null)
            {
                UnityEngine.Object.DestroyImmediate(_secondary);
            }

            if (_primary != null)
            {
                UnityEngine.Object.DestroyImmediate(_primary);
            }
        }

        [Test]
        public void Awake_ReachesReadyStateAndClaimsSingleton()
        {
            _primary = new GameObject("Bootstrap");
            var bootstrapper = _primary.AddComponent<GameBootstrapper>();

            Assert.AreEqual(BootstrapState.Ready, bootstrapper.State);
            Assert.AreSame(bootstrapper, GameBootstrapper.Instance);
        }

        [Test]
        public void Awake_RegistersCorePhase1Services()
        {
            _primary = new GameObject("Bootstrap");
            var bootstrapper = _primary.AddComponent<GameBootstrapper>();

            Assert.DoesNotThrow(() => bootstrapper.Services.Get<ILoggingService>());
            Assert.DoesNotThrow(() => bootstrapper.Services.Get<IGameStateService>());
            Assert.DoesNotThrow(() => bootstrapper.Services.Get<ISceneService>());
        }

        [Test]
        public void Initialize_CalledAgainAfterAwake_ThrowsInvalidOperationException()
        {
            _primary = new GameObject("Bootstrap");
            var bootstrapper = _primary.AddComponent<GameBootstrapper>();

            Assert.Throws<InvalidOperationException>(() => bootstrapper.Initialize());
        }

        [UnityTest]
        public IEnumerator Awake_SecondInstance_DestroysItselfAndKeepsFirstAsInstance()
        {
            _primary = new GameObject("Primary");
            var primaryBootstrapper = _primary.AddComponent<GameBootstrapper>();

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Duplicate GameBootstrapper.*"));

            _secondary = new GameObject("Secondary");
            _secondary.AddComponent<GameBootstrapper>();

            // Destroy() defers actual destruction until after this frame.
            yield return null;

            Assert.AreSame(primaryBootstrapper, GameBootstrapper.Instance);
            Assert.IsTrue(_secondary.IsNullOrDestroyed());
            _secondary = null;
        }

        [Test]
        public void Shutdown_TransitionsToShutdownAndClearsServices()
        {
            _primary = new GameObject("Bootstrap");
            var bootstrapper = _primary.AddComponent<GameBootstrapper>();

            bootstrapper.Shutdown();

            Assert.AreEqual(BootstrapState.Shutdown, bootstrapper.State);
            Assert.Throws<ServiceNotFoundException>(() => bootstrapper.Services.Get<ILoggingService>());
        }

        [Test]
        public void Shutdown_CalledTwice_IsANoOp()
        {
            _primary = new GameObject("Bootstrap");
            var bootstrapper = _primary.AddComponent<GameBootstrapper>();

            bootstrapper.Shutdown();
            Assert.DoesNotThrow(() => bootstrapper.Shutdown());
            Assert.AreEqual(BootstrapState.Shutdown, bootstrapper.State);
        }
    }
}
