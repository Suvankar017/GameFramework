using System;
using GameFramework.Runtime.SceneManagement;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace GameFramework.Runtime.Tests.SceneManagement
{
    // Scoped to behavior that doesn't require actually loading/unloading a scene, which would
    // disturb whatever scene the Editor (or test runner) currently has open. Real load/unload
    // round-trips are left to manual/integration testing in a dedicated scene, not unit tests.
    public class SceneServiceTests
    {
        private const string UnknownSceneName = "GameFramework_Tests_Nonexistent_Scene";

        private SceneService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new SceneService();
        }

        [Test]
        public void ActiveSceneName_DelegatesToUnitysActiveScene()
        {
            // Not asserting a specific non-empty name: the test runner's own scene context (e.g.
            // an untitled temp scene) may legitimately have an empty name. What must hold is that
            // this property always reflects whatever Unity currently reports as active.
            Assert.AreEqual(SceneManager.GetActiveScene().name, _service.ActiveSceneName);
        }

        [Test]
        public void IsLoaded_UnknownScene_ReturnsFalse()
        {
            Assert.IsFalse(_service.IsLoaded(UnknownSceneName));
        }

        [Test]
        public void IsLoadInProgress_SceneNeverRequested_ReturnsFalse()
        {
            Assert.IsFalse(_service.IsLoadInProgress(UnknownSceneName));
        }

        [TestCase(null)]
        [TestCase("")]
        public void IsLoaded_NullOrEmptySceneName_ThrowsArgumentException(string sceneName)
        {
            Assert.Throws<ArgumentException>(() => _service.IsLoaded(sceneName));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Load_NullOrEmptySceneName_ThrowsArgumentException(string sceneName)
        {
            Assert.Throws<ArgumentException>(() => _service.Load(sceneName));
        }

        [Test]
        public void Load_SceneNotInBuildSettings_ThrowsSceneNotFoundException()
        {
            Assert.Throws<SceneNotFoundException>(() => _service.Load(UnknownSceneName));
        }

        [Test]
        public void LoadAsync_SceneNotInBuildSettings_ThrowsSceneNotFoundException()
        {
            Assert.Throws<SceneNotFoundException>(() => _service.LoadAsync(UnknownSceneName));
        }

        [TestCase(null)]
        [TestCase("")]
        public void LoadAsync_NullOrEmptySceneName_ThrowsArgumentException(string sceneName)
        {
            Assert.Throws<ArgumentException>(() => _service.LoadAsync(sceneName));
        }

        [Test]
        public void UnloadAsync_SceneNotCurrentlyLoaded_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _service.UnloadAsync(UnknownSceneName));
        }

        [TestCase(null)]
        [TestCase("")]
        public void UnloadAsync_NullOrEmptySceneName_ThrowsArgumentException(string sceneName)
        {
            Assert.Throws<ArgumentException>(() => _service.UnloadAsync(sceneName));
        }

        [Test]
        public void Shutdown_ClearsInFlightLoadTracking()
        {
            _service.Initialize(null);

            _service.Shutdown();

            Assert.IsFalse(_service.IsLoadInProgress(UnknownSceneName));
        }
    }
}
