using System;
using GameFramework.Monetization.Entitlements;
using GameFramework.Rewards;
using UnityEngine;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>
    /// One product's authoring data - a plain serializable entry inside <see cref="ProductCatalog"/>
    /// rather than its own ScriptableObject asset (see <c>Ads.AdPlacementConfig</c>'s remarks for the
    /// same reasoning). <see cref="GrantedEntitlementId"/>/<see cref="GrantedRewardId"/> are both
    /// optional and independent - see CLAUDE.md's Phase 15 brief, section 57: a product typically
    /// sets exactly one, reusing Phase 6's <c>IRewardService</c>/this phase's
    /// <see cref="IEntitlementService"/> rather than a second reward format.
    /// </summary>
    [Serializable]
    public sealed class ProductDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private ProductType _type;
        [SerializeField] private string _androidProductId;
        [SerializeField] private string _iosProductId;

        [Tooltip("Shown until the provider's own localized catalog data has loaded (e.g. in the Editor, or before Initialize completes).")]
        [SerializeField] private string _fallbackDisplayName;

        [Tooltip("Optional - granted via IEntitlementService when this product is purchased/restored. Typically used for NonConsumable/Subscription.")]
        [SerializeField] private string _grantedEntitlementId;

        [Tooltip("Optional - claimed via Rewards.IRewardService when this product is purchased. Typically used for Consumable.")]
        [SerializeField] private string _grantedRewardId;

        public ProductId Id => new ProductId(_id);
        public ProductType Type => _type;
        public string FallbackDisplayName => _fallbackDisplayName;
        public EntitlementId GrantedEntitlementId => new EntitlementId(_grantedEntitlementId);
        public RewardId GrantedRewardId => new RewardId(_grantedRewardId);

        /// <summary>The authored Google Play product id. Phase 20: exposed read-only for build-time
        /// store-readiness validation; runtime code should keep using <see cref="ResolvePlatformProductId"/>.</summary>
        public string AndroidProductId => _androidProductId;

        /// <summary>The authored App Store product id. Phase 20: exposed read-only for build-time
        /// store-readiness validation; runtime code should keep using <see cref="ResolvePlatformProductId"/>.</summary>
        public string IosProductId => _iosProductId;

        /// <summary>Resolves the store product id for the running platform - Android/iOS as
        /// authored, or empty in the Editor/any other platform (the mock provider does not need a
        /// real store id).</summary>
        public string ResolvePlatformProductId()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _androidProductId;
#elif UNITY_IOS && !UNITY_EDITOR
            return _iosProductId;
#else
            return string.Empty;
#endif
        }
    }
}
