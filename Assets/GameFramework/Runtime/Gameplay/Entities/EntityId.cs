using System;
using System.Threading;

namespace GameFramework.Gameplay.Entities
{
    /// <summary>
    /// A persistent gameplay identity, distinct from <see cref="UnityEngine.Object.GetInstanceID"/>:
    /// an instance ID is a runtime-only handle that is reused after destruction and meaningless
    /// across a save/load or even across two runs of the same process, so it must never be treated
    /// as a stable gameplay identity. An <see cref="EntityId"/> is a monotonically increasing
    /// counter value, unique for the lifetime of the current process only — it is not itself
    /// persisted or guaranteed stable across sessions. A game that needs an identity to survive
    /// save/load must persist the assigned value itself through the Phase 2 Persistence service;
    /// this type intentionally does not invent a second identity/save mechanism.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        /// <summary>The invalid/unassigned id — the default value of this struct.</summary>
        public static readonly EntityId None = default;

        private static long _next;

        private readonly long _value;

        private EntityId(long value)
        {
            _value = value;
        }

        public bool IsValid => _value != 0;

        /// <summary>Allocates a new, process-unique id. Thread-safe.</summary>
        public static EntityId New() => new EntityId(Interlocked.Increment(ref _next));

        public bool Equals(EntityId other) => _value == other._value;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => IsValid ? _value.ToString() : "None";

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}
