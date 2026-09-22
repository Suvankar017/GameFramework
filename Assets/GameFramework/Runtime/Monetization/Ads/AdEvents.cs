namespace GameFramework.Monetization.Ads
{
    /// <summary>Published by <see cref="AdsService"/> through the Phase 2 Event System, mirroring
    /// its own C# events - Phase 16 (Analytics) and a game's UI/audio/feedback layers are the
    /// intended listeners (see CLAUDE.md's Phase 15 brief, sections 37/48/51).</summary>
    public readonly struct AdLoadedEvent
    {
        public readonly AdPlacementId PlacementId;
        public AdLoadedEvent(AdPlacementId placementId) => PlacementId = placementId;
    }

    public readonly struct AdLoadFailedEvent
    {
        public readonly AdPlacementId PlacementId;
        public readonly string FailureDetail;
        public AdLoadFailedEvent(AdPlacementId placementId, string failureDetail)
        {
            PlacementId = placementId;
            FailureDetail = failureDetail;
        }
    }

    public readonly struct AdShownEvent
    {
        public readonly AdPlacementId PlacementId;
        public AdShownEvent(AdPlacementId placementId) => PlacementId = placementId;
    }

    public readonly struct AdShowFailedEvent
    {
        public readonly AdPlacementId PlacementId;
        public readonly string FailureDetail;
        public AdShowFailedEvent(AdPlacementId placementId, string failureDetail)
        {
            PlacementId = placementId;
            FailureDetail = failureDetail;
        }
    }

    public readonly struct AdClosedEvent
    {
        public readonly AdPlacementId PlacementId;
        public AdClosedEvent(AdPlacementId placementId) => PlacementId = placementId;
    }

    /// <summary>Published only for <see cref="RewardedAdResult.RewardEarned"/> - never for a merely
    /// shown/closed rewarded ad. Carries no reward content; see CLAUDE.md's Phase 15 brief, section 8
    /// for why <see cref="AdsService"/> itself never grants a reward.</summary>
    public readonly struct AdRewardEarnedEvent
    {
        public readonly AdPlacementId PlacementId;
        public AdRewardEarnedEvent(AdPlacementId placementId) => PlacementId = placementId;
    }
}
