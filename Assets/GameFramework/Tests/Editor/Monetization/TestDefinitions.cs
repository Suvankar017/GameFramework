using System.Reflection;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Purchases;
using GameFramework.Rewards;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Monetization.Tests
{
    /// <summary>See <c>GameFramework.Rewards.Tests.TestDefinitions</c>'s remarks - same
    /// reflection-based private-field population, since none of these authoring types expose a
    /// public constructor (by design - see e.g. <c>AdPlacementConfig</c>'s remarks).</summary>
    internal static class TestDefinitions
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static AdPlacementConfig AdPlacement(string id, AdType type, float cooldownSeconds = 0f, int sessionShowLimit = 0)
        {
            var placement = new AdPlacementConfig();
            SetField(placement, "_id", id);
            SetField(placement, "_type", type);
            SetField(placement, "_cooldownSeconds", cooldownSeconds);
            SetField(placement, "_sessionShowLimit", sessionShowLimit);
            return placement;
        }

        public static AdEntitlementSuppressionRule SuppressionRule(string entitlementId, params AdType[] suppressedTypes)
        {
            var rule = new AdEntitlementSuppressionRule();
            SetField(rule, "_entitlementId", entitlementId);
            SetField(rule, "_suppressedTypes", suppressedTypes);
            return rule;
        }

        public static AdConfiguration AdConfig(AdPlacementConfig[] placements, AdEntitlementSuppressionRule[] suppressions = null)
        {
            var config = ScriptableObject.CreateInstance<AdConfiguration>();
            SetField(config, "_placements", placements);
            SetField(config, "_entitlementSuppressions", suppressions ?? System.Array.Empty<AdEntitlementSuppressionRule>());
            SetField(config, "_maxLoadRetries", 2);
            SetField(config, "_retryBackoffSeconds", 1f);
            return config;
        }

        public static ProductDefinition Product(string id, ProductType type, string entitlementId = null, string rewardId = null, string fallbackDisplayName = null)
        {
            var product = new ProductDefinition();
            SetField(product, "_id", id);
            SetField(product, "_type", type);
            SetField(product, "_grantedEntitlementId", entitlementId);
            SetField(product, "_grantedRewardId", rewardId);
            SetField(product, "_fallbackDisplayName", fallbackDisplayName ?? id);
            return product;
        }

        public static ProductCatalog Catalog(params ProductDefinition[] products)
        {
            var catalog = ScriptableObject.CreateInstance<ProductCatalog>();
            SetField(catalog, "_products", products);
            return catalog;
        }

        public static RewardDefinition Reward(string id, RewardClaimPolicy policy)
        {
            var definition = ScriptableObject.CreateInstance<RewardDefinition>();
            SetField(definition, "_id", id);
            SetField(definition, "_claimPolicy", policy);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, Flags);
            field.SetValue(target, value);
        }
    }
}
