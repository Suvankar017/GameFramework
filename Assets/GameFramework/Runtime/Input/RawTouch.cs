using UnityEngine;

namespace GameFramework.Input
{
    /// <summary>
    /// A minimal, testable snapshot of one touch. <see cref="UnityEngine.Touch"/> itself is not
    /// used as the sampling boundary type because its fields cannot be constructed with arbitrary
    /// values from test code — <see cref="IInputSampler"/> deals in this type instead, and only
    /// <see cref="UnityInputSampler"/> converts real <see cref="UnityEngine.Touch"/> values into it.
    /// </summary>
    public readonly struct RawTouch
    {
        public readonly int FingerId;
        public readonly Vector2 Position;
        public readonly TouchPhase Phase;

        public RawTouch(int fingerId, Vector2 position, TouchPhase phase)
        {
            FingerId = fingerId;
            Position = position;
            Phase = phase;
        }
    }
}
