using GameFramework.Runtime.Services;

namespace GameFramework.Unlocks
{
    /// <summary>
    /// Generic unlock tracking, evaluated against composable <see cref="IUnlockRequirement"/>s.
    /// The framework defines no concrete unlockable content — a game registers whatever
    /// <see cref="UnlockId"/>s it has, each with the requirement that must be satisfied to unlock it.
    /// </summary>
    public interface IUnlockService : IGameService
    {
        /// <summary>Registers an unlock definition and the requirement that must be satisfied to
        /// unlock it. Call once per unlock at composition-root time — not meant to be re-registered
        /// at runtime. <paramref name="requirement"/> may be null for an unlock that is granted only
        /// by explicit code (e.g. purely reward-driven, never player-initiated).</summary>
        void RegisterUnlock(UnlockDefinition definition, IUnlockRequirement requirement);

        bool IsRegistered(UnlockId id);

        bool IsUnlocked(UnlockId id);

        /// <summary>True if not already unlocked and every requirement is currently satisfied.
        /// False (never throws) for an unregistered id.</summary>
        bool CanUnlock(UnlockId id);

        /// <summary>Human-readable reason <see cref="CanUnlock"/> is false — the unmet
        /// requirement's <see cref="IUnlockRequirement.Describe"/>, or null if already unlocked/
        /// unlockable/unregistered.</summary>
        string GetBlockingReason(UnlockId id);

        UnlockResult TryUnlock(UnlockId id, string reason = null);

        /// <summary>Directly marks an id unlocked without checking its requirement (e.g. a
        /// <see cref="GameFramework.Rewards.UnlockReward"/> grant, or save-data restoration).
        /// Publishes <see cref="UnlockChangedEvent"/> if it wasn't already unlocked.</summary>
        UnlockResult ForceUnlock(UnlockId id, string reason = null);

        /// <summary>
        /// Walks every registered requirement tree for <see cref="PrerequisiteUnlockRequirement"/>
        /// edges and detects cycles (A needs B, B needs A, or longer chains). A cycle can never
        /// cause a crash or infinite loop at runtime (see
        /// <see cref="PrerequisiteUnlockRequirement"/>'s remarks) — it is a silent, permanent
        /// content deadlock instead, which is exactly why this authoring-time check exists. Returns
        /// the ids involved in the first cycle found, or an empty list if none exists.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<UnlockId> ValidateNoCycles();

        void Save();
        void Load();

        /// <summary>Clears every unlock back to locked without touching saved data on disk until
        /// <see cref="Save"/> is called. Development/testing use.</summary>
        void ResetToDefaults();
    }
}
