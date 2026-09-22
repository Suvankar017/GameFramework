namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// Outcome of <see cref="IAdsService.ShowRewarded"/> - see CLAUDE.md's Phase 15 brief, section 7:
    /// a reward must only ever be granted for <see cref="RewardEarned"/>, never merely because
    /// <c>Show</c>/<c>ShowRewarded</c> was called.
    /// </summary>
    public enum RewardedAdResult
    {
        /// <summary>The provider reported the reward condition was actually satisfied (the ad
        /// played to completion). Only this result should ever cause a reward to be granted.</summary>
        RewardEarned,

        /// <summary>The ad was shown and closed before the reward condition was met (the player
        /// skipped/backed out early) - no reward.</summary>
        ClosedWithoutReward,

        NotAvailable,
        NotInitialized,
        AlreadyShowing,
        SuppressedByPolicy,
        Failed
    }
}
