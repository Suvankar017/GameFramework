using UnityEngine;

namespace GameFramework.Feedback
{
    /// <summary>
    /// Backed by <see cref="Handheld.Vibrate"/> — the only haptic API Unity provides without a
    /// third-party native plugin. It has no strength/amplitude parameter, so every
    /// <see cref="HapticStrength"/> produces the same physical vibration; this is an honest
    /// platform limitation, not a missing feature of this provider. A game that needs
    /// per-strength amplitude/duration must integrate a native haptics plugin and provide its own
    /// <see cref="IHapticProvider"/> — the seam this interface exists for.
    /// </summary>
    public sealed class MobileHapticProvider : IHapticProvider
    {
        public bool IsSupported => Application.isMobilePlatform;

        public void Trigger(HapticStrength strength)
        {
            // Handheld only exists in Android/iOS player builds (and the Editor). Without this guard,
            // every standalone/WebGL player build of the framework fails to compile. Found by the
            // Phase 20 build pipeline's first real Windows build.
#if UNITY_ANDROID || UNITY_IOS
            if (IsSupported)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
