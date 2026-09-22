using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Centralized, platform-independent screen/orientation/safe-area access - see CLAUDE.md's
    /// Phase 14 brief, sections 13-17. Deliberately does not include a UI-side safe-area component;
    /// this is the platform/device information source a UI layer would consume (none exists yet in
    /// this project - see that brief's section 16). Changes are published via
    /// <see cref="Runtime.Events.IEventService"/> as <see cref="ScreenOrientationChangedEvent"/>/
    /// <see cref="ScreenSafeAreaChangedEvent"/>, since Unity provides no native callback for either.
    /// </summary>
    public interface IScreenService : IGameService
    {
        Rect SafeArea { get; }

        ScreenOrientation Orientation { get; }

        /// <summary>Sets <see cref="UnityEngine.Screen.orientation"/> directly. Only meaningful
        /// under Android/iOS orientation-locking build settings; on desktop/editor this changes the
        /// reported value but has no visual effect (an honest Unity platform limitation, not a
        /// missing feature here).</summary>
        void SetOrientation(ScreenOrientation orientation);
    }
}
