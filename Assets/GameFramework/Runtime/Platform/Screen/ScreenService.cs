using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Default <see cref="IScreenService"/>. Unity has no change callback for either
    /// <see cref="UnityEngine.Screen.orientation"/> or <see cref="UnityEngine.Screen.safeArea"/>, so
    /// <see cref="ScreenSignalDriver"/> compares both once per frame and this publishes an event only
    /// when either actually changes - the same cheap-comparison-in-a-driver pattern
    /// <c>Performance.Mobile.ApplicationLifecycleDriver</c> already established for callbacks/polling
    /// a plain C# service cannot receive directly (see CLAUDE.md's Phase 14 brief, section 15).
    /// </summary>
    public sealed class ScreenService : IScreenService
    {
        private IEventService _events;
        private ScreenSignalDriver _driver;

        public Rect SafeArea { get; private set; }

        public ScreenOrientation Orientation { get; private set; }

        public void Initialize(IServiceRegistry registry)
        {
            _events = registry.Get<IEventService>();

            SafeArea = Screen.safeArea;
            Orientation = Screen.orientation;

            var driverObject = new GameObject(nameof(ScreenSignalDriver)) { hideFlags = HideFlags.DontSave };
            _driver = driverObject.AddComponent<ScreenSignalDriver>();
            _driver.Owner = this;

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(driverObject);
            }
        }

        public void Shutdown()
        {
            if (_driver == null)
            {
                return;
            }

            GameObject driverObject = _driver.gameObject;
            if (Application.isPlaying)
            {
                Object.Destroy(driverObject);
            }
            else
            {
                Object.DestroyImmediate(driverObject);
            }

            _driver = null;
        }

        public void SetOrientation(ScreenOrientation orientation)
        {
            Screen.orientation = orientation;
        }

        /// <summary>Called once per frame by <see cref="ScreenSignalDriver"/>. Internal (rather than
        /// private) purely so the driver - a separate class, by necessity, since a plain C# service
        /// cannot receive <c>Update</c> - can call it; not part of the public API.</summary>
        internal void PollForChanges()
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea != SafeArea)
            {
                SafeArea = safeArea;
                _events.Publish(new ScreenSafeAreaChangedEvent(safeArea));
            }

            ScreenOrientation orientation = Screen.orientation;
            if (orientation != Orientation)
            {
                Orientation = orientation;
                _events.Publish(new ScreenOrientationChangedEvent(orientation));
            }
        }
    }
}
