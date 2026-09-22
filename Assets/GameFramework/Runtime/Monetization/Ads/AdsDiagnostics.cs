using System.Collections.Generic;

namespace GameFramework.Monetization.Ads
{
    /// <summary>Read-only development snapshot for a diagnostics menu/overlay - see CLAUDE.md's
    /// Phase 15 brief, section 38. Never used to drive gameplay logic.</summary>
    public readonly struct AdsDiagnostics
    {
        public readonly MonetizationProviderState State;
        public readonly IReadOnlyList<AdPlacementId> LoadedPlacements;
        public readonly IReadOnlyList<AdPlacementId> ActiveBanners;
        public readonly string LastError;

        public AdsDiagnostics(MonetizationProviderState state, IReadOnlyList<AdPlacementId> loadedPlacements, IReadOnlyList<AdPlacementId> activeBanners, string lastError)
        {
            State = state;
            LoadedPlacements = loadedPlacements;
            ActiveBanners = activeBanners;
            LastError = lastError;
        }
    }
}
