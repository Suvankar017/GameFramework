using System;
using GameFramework.Performance.Profiling;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.Performance.Ticking
{
    /// <summary>Default <see cref="ITickService"/>. See the interface and <see cref="ITickable"/>
    /// for the behavioral contract.</summary>
    public sealed class TickService : ITickService, IUpdatableService
    {
        private const string LogCategory = "Ticking";

        private readonly TickRegistry<ITickable> _tickables = new TickRegistry<ITickable>();
        private readonly TickRegistry<IFixedTickable> _fixedTickables = new TickRegistry<IFixedTickable>();
        private readonly TickRegistry<ILateTickable> _lateTickables = new TickRegistry<ILateTickable>();

        private ITimeService _time;
        private TickServiceDriver _driver;

        public int TickableCount => _tickables.Count;
        public int FixedTickableCount => _fixedTickables.Count;
        public int LateTickableCount => _lateTickables.Count;

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();

            var driverObject = new GameObject(nameof(TickServiceDriver)) { hideFlags = HideFlags.DontSave };
            _driver = driverObject.AddComponent<TickServiceDriver>();
            _driver.Owner = this;

            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(driverObject);
            }
        }

        public void Shutdown()
        {
            if (_driver != null)
            {
                DestroySafely(_driver.gameObject);
                _driver = null;
            }
        }

        private static void DestroySafely(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public void Register(ITickable tickable, int priority = 0, TickGroup group = TickGroup.Gameplay) =>
            _tickables.Register(tickable, priority, group);

        public void Unregister(ITickable tickable) => _tickables.Unregister(tickable);

        public void RegisterFixed(IFixedTickable tickable, int priority = 0, TickGroup group = TickGroup.Physics) =>
            _fixedTickables.Register(tickable, priority, group);

        public void UnregisterFixed(IFixedTickable tickable) => _fixedTickables.Unregister(tickable);

        public void RegisterLate(ILateTickable tickable, int priority = 0, TickGroup group = TickGroup.Presentation) =>
            _lateTickables.Register(tickable, priority, group);

        public void UnregisterLate(ILateTickable tickable) => _lateTickables.Unregister(tickable);

        /// <summary>Variable tick - called from <c>GameBootstrapper.Update()</c> via
        /// <see cref="IUpdatableService"/>, exactly like every other Phase 2+ per-frame service.</summary>
        public void Tick()
        {
            using (new ProfileScope(ProfilingCategory.Framework))
            {
                float deltaTime = _time.ScaledDeltaTime;
                ITickable[] buffer = _tickables.Snapshot(out int count);
                for (int i = 0; i < count; i++)
                {
                    InvokeSafe(buffer[i], deltaTime);
                }
            }
        }

        internal void TickFixed()
        {
            using (new ProfileScope(ProfilingCategory.Physics))
            {
                float fixedDeltaTime = _time.FixedDeltaTime;
                IFixedTickable[] buffer = _fixedTickables.Snapshot(out int count);
                for (int i = 0; i < count; i++)
                {
                    InvokeSafeFixed(buffer[i], fixedDeltaTime);
                }
            }
        }

        internal void TickLate()
        {
            using (new ProfileScope(ProfilingCategory.Rendering))
            {
                float deltaTime = _time.ScaledDeltaTime;
                ILateTickable[] buffer = _lateTickables.Snapshot(out int count);
                for (int i = 0; i < count; i++)
                {
                    InvokeSafeLate(buffer[i], deltaTime);
                }
            }
        }

        private static void InvokeSafe(ITickable tickable, float deltaTime)
        {
            try
            {
                tickable.Tick(deltaTime);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, LogCategory);
            }
        }

        private static void InvokeSafeFixed(IFixedTickable tickable, float fixedDeltaTime)
        {
            try
            {
                tickable.FixedTick(fixedDeltaTime);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, LogCategory);
            }
        }

        private static void InvokeSafeLate(ILateTickable tickable, float deltaTime)
        {
            try
            {
                tickable.LateTick(deltaTime);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, LogCategory);
            }
        }
    }
}
