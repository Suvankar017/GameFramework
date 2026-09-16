using System;
using System.Collections.Generic;
using GameFramework.Core.Extensions;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Default <see cref="IGameplayService"/>. Builds its own <c>DontDestroyOnLoad</c> driver
    /// GameObject in <see cref="Initialize"/> (the same self-contained, zero-scene-setup pattern
    /// <c>AudioService</c>/<c>UIService</c> use) purely to receive FixedUpdate/LateUpdate — see
    /// <see cref="GameplayLoopDriver"/>. Regular per-frame ticking runs through
    /// <see cref="IUpdatableService.Tick"/>, driven by <c>GameBootstrapper.Update()</c> like every
    /// other framework service.
    /// </summary>
    public sealed class GameplayService : IGameplayService, IUpdatableService
    {
        private const string LogCategory = "Gameplay";

        private readonly List<IGameplayLifecycle> _lifecycleParticipants = new List<IGameplayLifecycle>();
        private readonly List<IGameplayTickable> _tickables = new List<IGameplayTickable>();
        private readonly List<IGameplayFixedTickable> _fixedTickables = new List<IGameplayFixedTickable>();
        private readonly List<IGameplayLateTickable> _lateTickables = new List<IGameplayLateTickable>();

        private ITimeService _time;
        private ILoggingService _log;
        private GameObject _driverRoot;
        private bool _wasPaused;

        public GameplayLoopState State { get; private set; } = GameplayLoopState.Inactive;
        public bool IsActive => State == GameplayLoopState.Playing;

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();
            registry.TryGet(out _log);

            _driverRoot = new GameObject("GameplayService (GameFramework)");
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_driverRoot);
            }

            GameplayLoopDriver driver = _driverRoot.AddComponent<GameplayLoopDriver>();
            driver.Owner = this;
        }

        public void Shutdown()
        {
            if (State != GameplayLoopState.Inactive)
            {
                EndPlay();
            }

            _lifecycleParticipants.Clear();
            _tickables.Clear();
            _fixedTickables.Clear();
            _lateTickables.Clear();

            DestroySafely(_driverRoot);
            _driverRoot = null;
        }

        public void BeginPlay()
        {
            if (State != GameplayLoopState.Inactive)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "BeginPlay called while a session is already active; ignored.");
                return;
            }

            _wasPaused = _time.IsPaused;
            State = _wasPaused ? GameplayLoopState.Paused : GameplayLoopState.Playing;

            for (int i = 0; i < _lifecycleParticipants.Count; i++)
            {
                InvokeSafely(_lifecycleParticipants[i].OnGameplayBeginPlay, "OnGameplayBeginPlay");
            }

            if (_wasPaused)
            {
                for (int i = 0; i < _lifecycleParticipants.Count; i++)
                {
                    InvokeSafely(_lifecycleParticipants[i].OnGameplayPause, "OnGameplayPause");
                }
            }
        }

        public void EndPlay()
        {
            if (State == GameplayLoopState.Inactive)
            {
                _log?.Log(LogLevel.Warning, LogCategory, "EndPlay called while no session is active; ignored.");
                return;
            }

            State = GameplayLoopState.Inactive;

            for (int i = 0; i < _lifecycleParticipants.Count; i++)
            {
                InvokeSafely(_lifecycleParticipants[i].OnGameplayShutdown, "OnGameplayShutdown");
            }
        }

        public void RegisterLifecycle(IGameplayLifecycle participant)
        {
            if (participant == null)
            {
                return;
            }

            if (_lifecycleParticipants.Contains(participant))
            {
                _log?.Log(LogLevel.Warning, LogCategory, "RegisterLifecycle called with an already-registered participant; ignored.");
                return;
            }

            _lifecycleParticipants.Add(participant);
            InvokeSafely(participant.OnGameplayInitialize, "OnGameplayInitialize");

            if (State != GameplayLoopState.Inactive)
            {
                InvokeSafely(participant.OnGameplayBeginPlay, "OnGameplayBeginPlay");

                if (State == GameplayLoopState.Paused)
                {
                    InvokeSafely(participant.OnGameplayPause, "OnGameplayPause");
                }
            }
        }

        public void UnregisterLifecycle(IGameplayLifecycle participant)
        {
            if (participant == null || !_lifecycleParticipants.Remove(participant))
            {
                return;
            }

            InvokeSafely(participant.OnGameplayShutdown, "OnGameplayShutdown");
        }

        public void RegisterTickable(IGameplayTickable tickable)
        {
            if (tickable != null && !_tickables.Contains(tickable))
            {
                _tickables.Add(tickable);
            }
        }

        public void UnregisterTickable(IGameplayTickable tickable) => _tickables.Remove(tickable);

        public void RegisterFixedTickable(IGameplayFixedTickable tickable)
        {
            if (tickable != null && !_fixedTickables.Contains(tickable))
            {
                _fixedTickables.Add(tickable);
            }
        }

        public void UnregisterFixedTickable(IGameplayFixedTickable tickable) => _fixedTickables.Remove(tickable);

        public void RegisterLateTickable(IGameplayLateTickable tickable)
        {
            if (tickable != null && !_lateTickables.Contains(tickable))
            {
                _lateTickables.Add(tickable);
            }
        }

        public void UnregisterLateTickable(IGameplayLateTickable tickable) => _lateTickables.Remove(tickable);

        /// <summary>Regular Update-phase tick, driven by <c>GameBootstrapper.Update()</c> via
        /// <see cref="IUpdatableService"/>. Detects pause transitions and ticks
        /// <see cref="IGameplayTickable"/> participants while playing.</summary>
        public void Tick()
        {
            if (State == GameplayLoopState.Inactive)
            {
                return;
            }

            bool isPausedNow = _time.IsPaused;
            if (isPausedNow != _wasPaused)
            {
                _wasPaused = isPausedNow;
                State = isPausedNow ? GameplayLoopState.Paused : GameplayLoopState.Playing;

                for (int i = 0; i < _lifecycleParticipants.Count; i++)
                {
                    if (isPausedNow)
                    {
                        InvokeSafely(_lifecycleParticipants[i].OnGameplayPause, "OnGameplayPause");
                    }
                    else
                    {
                        InvokeSafely(_lifecycleParticipants[i].OnGameplayResume, "OnGameplayResume");
                    }
                }
            }

            if (State != GameplayLoopState.Playing)
            {
                return;
            }

            float deltaTime = _time.ScaledDeltaTime;
            for (int i = 0; i < _tickables.Count; i++)
            {
                InvokeSafely(_tickables[i], deltaTime);
            }
        }

        internal void TickFixed()
        {
            if (State != GameplayLoopState.Playing)
            {
                return;
            }

            float fixedDeltaTime = _time.FixedDeltaTime;
            for (int i = 0; i < _fixedTickables.Count; i++)
            {
                InvokeSafely(_fixedTickables[i], fixedDeltaTime);
            }
        }

        internal void TickLate()
        {
            if (State != GameplayLoopState.Playing)
            {
                return;
            }

            float deltaTime = _time.ScaledDeltaTime;
            for (int i = 0; i < _lateTickables.Count; i++)
            {
                InvokeSafely(_lateTickables[i], deltaTime);
            }
        }

        private void InvokeSafely(Action action, string phase)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _log?.LogException(ex, LogCategory);
                _log?.Log(LogLevel.Error, LogCategory, $"Gameplay lifecycle participant threw during {phase}.");
            }
        }

        private void InvokeSafely(IGameplayTickable tickable, float deltaTime)
        {
            try
            {
                tickable.GameplayTick(deltaTime);
            }
            catch (Exception ex)
            {
                _log?.LogException(ex, LogCategory);
            }
        }

        private void InvokeSafely(IGameplayFixedTickable tickable, float fixedDeltaTime)
        {
            try
            {
                tickable.GameplayFixedTick(fixedDeltaTime);
            }
            catch (Exception ex)
            {
                _log?.LogException(ex, LogCategory);
            }
        }

        private void InvokeSafely(IGameplayLateTickable tickable, float deltaTime)
        {
            try
            {
                tickable.GameplayLateTick(deltaTime);
            }
            catch (Exception ex)
            {
                _log?.LogException(ex, LogCategory);
            }
        }

        private static void DestroySafely(GameObject go)
        {
            if (go.IsNullOrDestroyed())
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
