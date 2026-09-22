using UnityEngine;

namespace GameFramework.Monetization.Purchases
{
    /// <summary>Reusable, game-authored product catalog - see CLAUDE.md's Phase 15 brief, section 9/16:
    /// no store product id ever appears in gameplay code, only here.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Monetization/Product Catalog", fileName = "ProductCatalog")]
    public sealed class ProductCatalog : ScriptableObject
    {
        [SerializeField] private ProductDefinition[] _products = System.Array.Empty<ProductDefinition>();

        public ProductDefinition[] Products => _products;
    }
}
