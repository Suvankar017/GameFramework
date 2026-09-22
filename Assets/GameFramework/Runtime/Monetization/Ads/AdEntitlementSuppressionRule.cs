using System;
using GameFramework.Monetization.Entitlements;
using UnityEngine;

namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// "Owning this entitlement suppresses these ad types" - the mechanism behind CLAUDE.md's
    /// Phase 15 brief, section 20/21: Remove Ads is not hard-coded to mean "no rewarded ads." A game
    /// authors one rule suppressing Banner/Interstitial while leaving Rewarded untouched, or a
    /// different combination entirely.
    /// </summary>
    [Serializable]
    public sealed class AdEntitlementSuppressionRule
    {
        [SerializeField] private string _entitlementId;
        [SerializeField] private AdType[] _suppressedTypes = Array.Empty<AdType>();

        public EntitlementId EntitlementId => new EntitlementId(_entitlementId);
        public AdType[] SuppressedTypes => _suppressedTypes;
    }
}
