using System;
using System.Collections.Generic;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.GameFlow.Tests
{
    /// <summary>
    /// Test double for <see cref="ISceneService"/>. A real <c>AsyncOperation</c> cannot be
    /// constructed or driven manually outside of an actual Unity scene load, which is exactly why
    /// <see cref="GameFlowService"/> is designed to detect load completion through
    /// <see cref="SceneLoaded"/> instead (see its own remarks) - this fake lets tests fire that
    /// event on demand via <see cref="RaiseSceneLoaded"/>, with no engine scene loading involved.
    /// </summary>
    internal sealed class FakeSceneService : ISceneService
    {
        private readonly HashSet<string> _loaded = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _inFlight = new HashSet<string>(StringComparer.Ordinal);

        public string ActiveSceneName { get; set; } = "FakeActiveScene";
        public int LoadAsyncCallCount { get; private set; }
        public string LastRequestedSceneName { get; private set; }

        public event Action<string> SceneLoaded;
        public event Action<string> SceneUnloaded;

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public bool IsLoaded(string sceneName) => _loaded.Contains(sceneName);

        public bool IsLoadInProgress(string sceneName) => _inFlight.Contains(sceneName);

        public void Load(string sceneName)
        {
            _loaded.Add(sceneName);
            ActiveSceneName = sceneName;
        }

        public AsyncOperation LoadAsync(string sceneName, SceneLoadMode mode = SceneLoadMode.Single, bool activateOnLoad = true)
        {
            LoadAsyncCallCount++;
            LastRequestedSceneName = sceneName;
            _inFlight.Add(sceneName);
            return null;
        }

        public AsyncOperation UnloadAsync(string sceneName)
        {
            _loaded.Remove(sceneName);
            return null;
        }

        /// <summary>Simulates the scene finishing loading, exactly as the real <c>SceneService</c>
        /// would raise it from Unity's own <c>SceneManager.sceneLoaded</c>.</summary>
        public void RaiseSceneLoaded(string sceneName)
        {
            _inFlight.Remove(sceneName);
            _loaded.Add(sceneName);
            ActiveSceneName = sceneName;
            SceneLoaded?.Invoke(sceneName);
        }

        public void RaiseSceneUnloaded(string sceneName)
        {
            _loaded.Remove(sceneName);
            SceneUnloaded?.Invoke(sceneName);
        }
    }
}
