using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Time;
using UnityEngine;

namespace GameFramework.Performance.Mobile
{
    /// <summary>
    /// Default <see cref="IApplicationLifecycleService"/>. On <c>OnApplicationPause(true)</c> (the
    /// OS backgrounding the app - a phone call, the home button, another app taking focus) this
    /// calls <see cref="ITimeService.Pause"/>, and <see cref="ITimeService.Resume"/> on the matching
    /// unpause - reusing Phase 2's existing reference-counted pause rather than inventing a second
    /// pause mechanism, and directly satisfying "gameplay ticks stop while backgrounded" (battery)
    /// without every gameplay system needing its own <c>OnApplicationPause</c> handler. Because
    /// <see cref="ITimeService.Pause"/> is reference-counted, this composes correctly with a game's
    /// own pause menu calling Pause/Resume independently - one Resume can never cancel the other's
    /// still-active Pause.
    /// </summary>
    public sealed class ApplicationLifecycleService : IApplicationLifecycleService
    {
        private ITimeService _time;
        private IEventService _events;
        private ApplicationLifecycleDriver _driver;
        private bool _pausedByApplication;

        public bool IsPaused { get; private set; }

        public bool HasFocus { get; private set; } = true;

        public void Initialize(IServiceRegistry registry)
        {
            _time = registry.Get<ITimeService>();
            _events = registry.Get<IEventService>();

            var driverObject = new GameObject(nameof(ApplicationLifecycleDriver)) { hideFlags = HideFlags.DontSave };
            _driver = driverObject.AddComponent<ApplicationLifecycleDriver>();
            _driver.Owner = this;

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(driverObject);
            }
        }

        public void Shutdown()
        {
            if (_pausedByApplication)
            {
                _time.Resume();
                _pausedByApplication = false;
            }

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
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }

        internal void HandleApplicationPause(bool isPaused)
        {
            IsPaused = isPaused;

            if (isPaused && !_pausedByApplication)
            {
                _pausedByApplication = true;
                _time.Pause();
                _events.Publish(new ApplicationPausedEvent());
            }
            else if (!isPaused && _pausedByApplication)
            {
                _pausedByApplication = false;
                _time.Resume();
                _events.Publish(new ApplicationResumedEvent());
            }
        }

        internal void HandleApplicationFocus(bool hasFocus)
        {
            HasFocus = hasFocus;
            _events.Publish(new ApplicationFocusChangedEvent(hasFocus));
        }

        internal void HandleApplicationQuit()
        {
            _events.Publish(new ApplicationQuittingEvent());
        }
    }
}
