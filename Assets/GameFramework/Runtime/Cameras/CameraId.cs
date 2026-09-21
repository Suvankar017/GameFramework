using System;
using System.Threading;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Stable identity for a registered <see cref="CameraController"/> - a process-unique
    /// monotonic counter, the same shape as <c>Gameplay.Entities.EntityId</c> and explicitly not
    /// <see cref="UnityEngine.Object.GetInstanceID"/> (reused after destruction, meaningless
    /// across a save/load) and not the GameObject's name (see CLAUDE.md section 42).
    /// </summary>
    public readonly struct CameraId : IEquatable<CameraId>
    {
        /// <summary>The invalid/unassigned id - the default value of this struct.</summary>
        public static readonly CameraId None = default;

        private static long _next;

        private readonly long _value;

        private CameraId(long value)
        {
            _value = value;
        }

        public bool IsValid => _value != 0;

        /// <summary>Allocates a new, process-unique id. Thread-safe.</summary>
        public static CameraId New() => new CameraId(Interlocked.Increment(ref _next));

        public bool Equals(CameraId other) => _value == other._value;
        public override bool Equals(object obj) => obj is CameraId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => IsValid ? _value.ToString() : "None";

        public static bool operator ==(CameraId left, CameraId right) => left.Equals(right);
        public static bool operator !=(CameraId left, CameraId right) => !left.Equals(right);
    }
}
