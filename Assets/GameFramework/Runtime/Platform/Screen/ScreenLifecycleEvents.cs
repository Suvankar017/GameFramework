using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>Published by <see cref="ScreenService"/> through the Phase 2 Event System when a
    /// per-frame comparison (see <see cref="ScreenSignalDriver"/>) detects a change - Unity exposes
    /// no native callback for either.</summary>
    public readonly struct ScreenOrientationChangedEvent
    {
        public readonly ScreenOrientation Orientation;
        public ScreenOrientationChangedEvent(ScreenOrientation orientation) => Orientation = orientation;
    }

    public readonly struct ScreenSafeAreaChangedEvent
    {
        public readonly Rect SafeArea;
        public ScreenSafeAreaChangedEvent(Rect safeArea) => SafeArea = safeArea;
    }
}
