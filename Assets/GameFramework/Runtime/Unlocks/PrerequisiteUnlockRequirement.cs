using GameFramework.Core.Validation;

namespace GameFramework.Unlocks
{
    /// <summary>
    /// Checks that another unlock has already been granted. Checks the already-unlocked flag
    /// (<see cref="IUnlockService.IsUnlocked"/>), never re-evaluates the target's own requirement —
    /// this is what makes a requirement cycle (A needs B, B needs A) a safe, detectable permanent
    /// deadlock rather than infinite recursion: neither ever gets unlocked, but nothing ever
    /// recurses to find that out. See <see cref="UnlockService.ValidateNoCycles"/> for the
    /// authoring-time check that catches this content mistake before it ships.
    /// </summary>
    public sealed class PrerequisiteUnlockRequirement : IUnlockRequirement, IPrerequisiteRequirement
    {
        private readonly IUnlockService _unlocks;
        private readonly UnlockId _targetId;

        public PrerequisiteUnlockRequirement(IUnlockService unlocks, UnlockId targetId)
        {
            _unlocks = Guard.NotNull(unlocks, nameof(unlocks));
            _targetId = targetId;
        }

        public UnlockId TargetId => _targetId;

        public bool IsSatisfied() => _unlocks.IsUnlocked(_targetId);

        public string Describe() => $"Unlock '{_targetId}' first";
    }
}
