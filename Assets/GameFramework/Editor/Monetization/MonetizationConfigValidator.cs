using System.Collections.Generic;
using GameFramework.Monetization.Ads;
using GameFramework.Monetization.Purchases;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Monetization
{
    /// <summary>
    /// Authoring-time checks for Phase 15 content assets - see CLAUDE.md's Phase 15 brief, section 42.
    /// Follows the same "scan every asset of a type in the project" pattern as
    /// <c>Quests.QuestContentValidator</c>. Warnings only (never hard errors) - a missing
    /// entitlement/reward mapping or platform id is frequently correct while content is still being
    /// authored.
    /// </summary>
    internal static class MonetizationConfigValidator
    {
        [MenuItem("GameFramework/Monetization/Validate Configuration")]
        private static void ValidateAll()
        {
            int issues = 0;
            issues += ValidateAdConfigurations();
            issues += ValidateProductCatalogs();

            Debug.Log(issues == 0
                ? "[Monetization] Configuration validated with no issues."
                : $"[Monetization] Configuration validation found {issues} issue(s). See warnings above.");
        }

        private static int ValidateAdConfigurations()
        {
            int issues = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AdConfiguration)}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<AdConfiguration>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null)
                {
                    continue;
                }

                var seenIds = new HashSet<string>();
                foreach (AdPlacementConfig placement in config.Placements)
                {
                    if (!placement.Id.IsValid)
                    {
                        Debug.LogWarning($"[Monetization] AdConfiguration '{config.name}' has a placement with no Id assigned.", config);
                        issues++;
                        continue;
                    }

                    if (!seenIds.Add(placement.Id.Value))
                    {
                        Debug.LogWarning($"[Monetization] AdConfiguration '{config.name}' has duplicate placement Id '{placement.Id}'.", config);
                        issues++;
                    }
                }

                foreach (AdEntitlementSuppressionRule rule in config.EntitlementSuppressions)
                {
                    if (!rule.EntitlementId.IsValid)
                    {
                        Debug.LogWarning($"[Monetization] AdConfiguration '{config.name}' has an entitlement suppression rule with no EntitlementId assigned.", config);
                        issues++;
                    }
                }
            }

            return issues;
        }

        private static int ValidateProductCatalogs()
        {
            int issues = 0;

            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(ProductCatalog)}"))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ProductCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                if (catalog == null)
                {
                    continue;
                }

                var seenIds = new HashSet<string>();
                foreach (ProductDefinition product in catalog.Products)
                {
                    if (!product.Id.IsValid)
                    {
                        Debug.LogWarning($"[Monetization] ProductCatalog '{catalog.name}' has a product with no Id assigned.", catalog);
                        issues++;
                        continue;
                    }

                    if (!seenIds.Add(product.Id.Value))
                    {
                        Debug.LogWarning($"[Monetization] ProductCatalog '{catalog.name}' has duplicate product Id '{product.Id}'.", catalog);
                        issues++;
                    }

                    bool hasEntitlement = product.GrantedEntitlementId.IsValid;
                    bool hasReward = product.GrantedRewardId.IsValid;

                    if (!hasEntitlement && !hasReward)
                    {
                        Debug.LogWarning($"[Monetization] Product '{product.Id}' in '{catalog.name}' grants neither an Entitlement nor a Reward - purchasing it will have no effect.", catalog);
                        issues++;
                    }

                    if (product.Type == ProductType.Consumable && hasEntitlement)
                    {
                        Debug.LogWarning($"[Monetization] Product '{product.Id}' in '{catalog.name}' is Consumable but grants an Entitlement - entitlements are typically for NonConsumable/Subscription products.", catalog);
                        issues++;
                    }
                }
            }

            return issues;
        }
    }
}
