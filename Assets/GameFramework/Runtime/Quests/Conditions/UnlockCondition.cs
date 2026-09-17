using GameFramework.Core.Validation;
using GameFramework.Unlocks;

namespace GameFramework.Quests.Conditions
{
    /// <summary>Checks that content has already been unlocked (e.g. gating a quest's availability on
    /// a prior unlock, or an objective for "Unlock character X"). No progress concept applies -
    /// unlocking is binary.</summary>
    public sealed class UnlockCondition : ICondition
    {
        private readonly IUnlockService _unlocks;
        private readonly UnlockId _unlockId;

        public UnlockCondition(IUnlockService unlocks, UnlockId unlockId)
        {
            _unlocks = Guard.NotNull(unlocks, nameof(unlocks));
            _unlockId = unlockId;
        }

        public bool IsSatisfied() => _unlocks.IsUnlocked(_unlockId);

        public string Describe() => $"Unlock '{_unlockId}'";
    }
}
