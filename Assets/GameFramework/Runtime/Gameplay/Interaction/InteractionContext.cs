using UnityEngine;

namespace GameFramework.Gameplay.Interaction
{
    /// <summary>Lightweight, deliberately minimal data for one interaction attempt. Does not carry
    /// input state or a free-form context slot — a game needing more composes its own layer around
    /// this rather than the framework growing a "universal interaction object".</summary>
    public readonly struct InteractionContext
    {
        public readonly GameObject Interactor;
        public readonly GameObject Target;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;

        public InteractionContext(GameObject interactor, GameObject target, Vector3 position, Vector3 direction)
        {
            Interactor = interactor;
            Target = target;
            Position = position;
            Direction = direction;
        }
    }
}
