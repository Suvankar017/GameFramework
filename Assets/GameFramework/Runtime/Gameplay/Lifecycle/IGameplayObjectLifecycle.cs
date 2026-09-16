namespace GameFramework.Gameplay.Lifecycle
{
    /// <summary>
    /// Optional per-object lifecycle contract, distinct from <see cref="IGameplayLifecycle"/>
    /// (which is about the global gameplay session). This one is about a single reusable object's
    /// own lifecycle, and is written so it behaves correctly whether the object is ever pooled or
    /// not: <see cref="Initialize"/> fires once no matter how many times the object is later
    /// reused, <see cref="Activate"/>/<see cref="Deactivate"/> fire every time it starts/stops
    /// being in play (fresh instantiation, or a pool Get/Release), and <see cref="Dispose"/> fires
    /// once, right before real destruction — never on a pool Release. Drive this through
    /// <see cref="GameplayObjectLifecycleRunner"/> rather than calling these methods directly, to
    /// get the "each phase fires only in the right order, at most as documented" guarantee for free.
    /// A simple object with no meaningful state does not need to implement this at all — a pool
    /// works fine on a prefab with none of its components implementing it.
    /// </summary>
    public interface IGameplayObjectLifecycle
    {
        /// <summary>Called once, the first time this object exists (Created → Initialized). Cache
        /// component references, subscribe to permanent state here.</summary>
        void Initialize();

        /// <summary>Called every time this object starts being in play: once after
        /// <see cref="Initialize"/> on first use, and again on every later pool Get. Reset
        /// per-use gameplay state here (health, timers, flags).</summary>
        void Activate();

        /// <summary>Called every time this object stops being in play: before a pool Release, or
        /// before destruction if never pooled. Unsubscribe events, cancel owned timers, clear
        /// runtime references acquired in <see cref="Activate"/> here.</summary>
        void Deactivate();

        /// <summary>Called once, immediately before the object is actually destroyed — never
        /// called for a pool Release. Release permanent resources acquired in
        /// <see cref="Initialize"/> here.</summary>
        void Dispose();
    }
}
