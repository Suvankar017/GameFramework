using System.Collections;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.SceneManagement;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.State;
using UnityEngine;

namespace GameFramework.Samples.Phase1Demo
{
    /// <summary>
    /// Drives the Phase 1 demonstration scene: exercises Bootstrap, Services, Logging, GameState,
    /// and SceneManagement together, with no authored assets required. Not part of the reusable
    /// framework - sample/demo content only, kept in its own assembly and scene, separate from any
    /// production game content.
    /// </summary>
    public sealed class Phase1DemoController : MonoBehaviour
    {
        private const string SubSceneName = "Phase1Demo_SubScene";

        private IGameStateService _gameState;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;

            var logging = services.Get<ILoggingService>();
            logging.Log(LogLevel.Info, "Phase1Demo", "Logging service resolved directly from the registry.");
            Log.Info("Phase1Demo", "The static Log facade reaches the same bound service.");

            _gameState = services.Get<IGameStateService>();
            _gameState.RegisterState("Menu", new DemoState("Menu"));
            _gameState.RegisterState("Gameplay", new DemoState("Gameplay"));
            _gameState.StateChanged += OnStateChanged;

            _gameState.TransitionTo("Menu");
            _gameState.TransitionTo("Gameplay");

            Debug.Log("[Phase1Demo] Press Space to transition GameState between Menu and Gameplay.");

            yield return DemoSceneManagement(services.Get<ISceneService>());

            Debug.Log("[Phase1Demo] Ready. GameState transitions still respond to Space.");
        }

        private void Update()
        {
            if (_gameState == null)
            {
                return; // Start's coroutine hasn't reached Ready yet.
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                string next = _gameState.CurrentState == "Menu" ? "Gameplay" : "Menu";
                _gameState.TransitionTo(next);
            }
        }

        private static IEnumerator DemoSceneManagement(ISceneService scenes)
        {
            void OnLoaded(string name) => Debug.Log($"[Phase1Demo] SceneService reported SceneLoaded: '{name}'.");
            void OnUnloaded(string name) => Debug.Log($"[Phase1Demo] SceneService reported SceneUnloaded: '{name}'.");

            scenes.SceneLoaded += OnLoaded;
            scenes.SceneUnloaded += OnUnloaded;

            Debug.Log($"[Phase1Demo] Loading '{SubSceneName}' additively...");
            AsyncOperation load = scenes.LoadAsync(SubSceneName, SceneLoadMode.Additive);
            yield return load;

            Debug.Log($"[Phase1Demo] '{SubSceneName}' loaded: {scenes.IsLoaded(SubSceneName)}. Unloading in 2 seconds...");
            yield return new WaitForSeconds(2f);

            AsyncOperation unload = scenes.UnloadAsync(SubSceneName);
            yield return unload;

            scenes.SceneLoaded -= OnLoaded;
            scenes.SceneUnloaded -= OnUnloaded;
        }

        private static void OnStateChanged(string previous, string current)
        {
            Debug.Log($"[Phase1Demo] GameState changed: '{previous ?? "<none>"}' -> '{current}'.");
        }

        /// <summary>Minimal <see cref="IGameState"/> that only logs its own Enter/Exit - the
        /// framework defines no concrete states, so a demo (or a game) always supplies its own.</summary>
        private sealed class DemoState : IGameState
        {
            private readonly string _name;

            public DemoState(string name)
            {
                _name = name;
            }

            public void Enter() => Debug.Log($"[Phase1Demo] State '{_name}' entered.");
            public void Exit() => Debug.Log($"[Phase1Demo] State '{_name}' exited.");
        }
    }
}
