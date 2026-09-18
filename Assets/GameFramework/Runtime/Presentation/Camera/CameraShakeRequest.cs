using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>One shake instance handed to <see cref="ICameraFeedbackDriver.RequestShake"/> -
    /// built from a <see cref="CameraFeedbackConfig"/> plus a request's intensity, or constructed
    /// directly for a one-off effect.</summary>
    public readonly struct CameraShakeRequest
    {
        public readonly float Amplitude;
        public readonly float Frequency;
        public readonly float Duration;
        public readonly AnimationCurve Falloff;
        public readonly Vector3 AxisMask;

        /// <summary>Seeds the deterministic noise sampling - two requests with the same seed and
        /// the same other parameters produce the exact same offset at the same elapsed time (see
        /// <see cref="CameraShakeState"/>'s remarks on determinism). Left null, a random seed is
        /// picked, which is the right default for real gameplay use but must be supplied explicitly
        /// by a test that asserts a specific output.</summary>
        public readonly int? Seed;

        public CameraShakeRequest(float amplitude, float frequency, float duration, AnimationCurve falloff = null, Vector3? axisMask = null, int? seed = null)
        {
            Amplitude = Mathf.Max(0f, amplitude);
            Frequency = Mathf.Max(0.01f, frequency);
            Duration = Mathf.Max(0f, duration);
            Falloff = falloff;
            AxisMask = axisMask ?? Vector3.one;
            Seed = seed;
        }
    }
}
