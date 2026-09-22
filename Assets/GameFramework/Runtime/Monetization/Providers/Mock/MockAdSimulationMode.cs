namespace GameFramework.Monetization.Providers.Mock
{
    /// <summary>Deterministic behavior for <see cref="MockAdProvider"/> - see CLAUDE.md's Phase 15
    /// brief, sections 39/44/67: automated tests and Editor play must not depend on a real ad
    /// network.</summary>
    public enum MockAdSimulationMode
    {
        /// <summary>Every Load succeeds; every Show succeeds, and every Rewarded show earns its
        /// reward before closing.</summary>
        AlwaysSucceed,

        /// <summary>Every Load fails.</summary>
        AlwaysFailToLoad,

        /// <summary>Loads succeed, but every Show fails.</summary>
        AlwaysFailToShow,

        /// <summary>Rewarded shows succeed but close before the reward condition is met - the
        /// "player skipped/backed out" case.</summary>
        RewardedClosesWithoutReward
    }
}
