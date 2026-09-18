using System;

namespace GameFramework.GameFlow.Session
{
    /// <summary>
    /// Process-lifetime-unique gameplay session identifier - the same shape as
    /// <c>GameFramework.Gameplay.Entities.EntityId</c>, deliberately not
    /// <see cref="UnityEngine.Object.GetInstanceID"/> and not itself persisted. A game that needs a
    /// resumable session to survive a save/load persists whatever it needs itself (typically just
    /// the active <see cref="LevelId"/>/checkpoint - see <see cref="GameFlowService"/>'s remarks on
    /// persistence) rather than this id, which is only ever meaningful within one run of the
    /// process.
    /// </summary>
    public readonly struct SessionId : IEquatable<SessionId>
    {
        public static readonly SessionId None = default;

        private static long _next;

        private readonly long _value;

        private SessionId(long value)
        {
            _value = value;
        }

        public bool IsValid => _value != 0;

        internal static SessionId New() => new SessionId(++_next);

        public bool Equals(SessionId other) => _value == other._value;
        public override bool Equals(object obj) => obj is SessionId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => IsValid ? _value.ToString() : "None";

        public static bool operator ==(SessionId left, SessionId right) => left.Equals(right);
        public static bool operator !=(SessionId left, SessionId right) => !left.Equals(right);
    }
}
