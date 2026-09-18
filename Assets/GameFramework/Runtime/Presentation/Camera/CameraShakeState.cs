using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Pure, Unity-lifecycle-free accumulator behind <see cref="CameraFeedbackDriver"/> - kept
    /// separate specifically so shake composition/falloff/determinism are unit-testable in EditMode
    /// without a live Camera or scene (mirroring how <see cref="TutorialLifecycleStateMachine"/> and
    /// <see cref="Gameplay.Objectives.ObjectiveBase"/> keep their state machines free of Unity
    /// lifecycle too).
    ///
    /// Multiple simultaneous shakes are composable by design (CLAUDE.md's Phase 10 brief, section
    /// 11): each tick sums every still-active shake's own offset rather than one replacing another.
    /// Each shake's offset is deterministic Perlin noise seeded by <see cref="CameraShakeRequest.Seed"/> -
    /// the same seed, amplitude, frequency, falloff, and elapsed time always produce the same
    /// offset, satisfying CLAUDE.md's Phase 10 brief section 36's determinism requirement.
    /// </summary>
    internal sealed class CameraShakeState
    {
        private struct ActiveShake
        {
            public float Amplitude;
            public float Frequency;
            public float Duration;
            public float Elapsed;
            public AnimationCurve Falloff;
            public Vector3 AxisMask;
            public int Seed;
        }

        private readonly List<ActiveShake> _active = new List<ActiveShake>();

        public int ActiveCount => _active.Count;

        public void Add(CameraShakeRequest request)
        {
            if (request.Duration <= 0f || request.Amplitude <= 0f)
            {
                return;
            }

            _active.Add(new ActiveShake
            {
                Amplitude = request.Amplitude,
                Frequency = request.Frequency,
                Duration = request.Duration,
                Elapsed = 0f,
                Falloff = request.Falloff,
                AxisMask = request.AxisMask,
                Seed = request.Seed ?? Random.Range(int.MinValue, int.MaxValue)
            });
        }

        public void CancelAll() => _active.Clear();

        /// <summary>Advances every active shake by <paramref name="deltaTime"/>, removes any that
        /// finished, and returns the combined offset for this tick - zero-allocation steady state
        /// (backward iteration, in-place struct mutation, no LINQ).</summary>
        public Vector3 Tick(float deltaTime)
        {
            Vector3 combined = Vector3.zero;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveShake shake = _active[i];
                shake.Elapsed += deltaTime;

                if (shake.Elapsed >= shake.Duration)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                _active[i] = shake;
                combined += ComputeOffset(shake);
            }

            return combined;
        }

        private static Vector3 ComputeOffset(in ActiveShake shake)
        {
            float t = Mathf.Clamp01(shake.Elapsed / shake.Duration);
            float falloff = shake.Falloff != null ? shake.Falloff.Evaluate(t) : 1f - t;
            float strength = shake.Amplitude * falloff;

            // Classic Perlin noise is exactly 0 at integer lattice points - sampling at whole-number
            // coordinates (an integer Seed plus a sampleTime that lands on a whole number, against a
            // constant 0 second axis) would silently collapse to the same degenerate ~0 output for
            // *every* seed. The golden-ratio-derived offset and distinct per-axis fractional
            // constants below keep every sample off the lattice and decorrelate the three axes.
            float sampleTime = shake.Elapsed * shake.Frequency;
            float seedOffset = shake.Seed * 0.06180339887f;

            float x = Mathf.PerlinNoise(seedOffset + sampleTime, 0.37f) * 2f - 1f;
            float y = Mathf.PerlinNoise(1.91f, seedOffset + sampleTime + 2.63f) * 2f - 1f;
            float z = Mathf.PerlinNoise(seedOffset + sampleTime + 4.71f, seedOffset - sampleTime + 5.19f) * 2f - 1f;

            return Vector3.Scale(new Vector3(x, y, z) * strength, shake.AxisMask);
        }
    }
}
