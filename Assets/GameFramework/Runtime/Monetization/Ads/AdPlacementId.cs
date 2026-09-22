using System;

namespace GameFramework.Monetization.Ads
{
    /// <summary>Stable, designer-authored placement key (e.g. "RewardedRevive",
    /// "LevelCompleteInterstitial") - see CLAUDE.md's Phase 15 brief, section 10. Game code requests
    /// a placement, never a raw provider ad-unit id.</summary>
    public readonly struct AdPlacementId : IEquatable<AdPlacementId>
    {
        public readonly string Value;

        public AdPlacementId(string value)
        {
            Value = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(AdPlacementId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AdPlacementId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(AdPlacementId left, AdPlacementId right) => left.Equals(right);
        public static bool operator !=(AdPlacementId left, AdPlacementId right) => !left.Equals(right);
    }
}
