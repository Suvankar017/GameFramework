using UnityEngine;

namespace GameFramework.Progression.Inventory
{
    /// <summary>
    /// Pure authoring data for one item — identity/display/stacking only, never mutated at runtime
    /// (see <see cref="InventoryService"/> for where owned quantities actually live).
    /// <see cref="Category"/> is a free-form string rather than a closed enum deliberately — the
    /// framework does not hard-code game-specific item categories (Consumable/Equipment/Vehicle/
    /// ...); a game defines whatever categories it needs.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Progression/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Tooltip("0 or negative = unlimited stack size.")]
        [SerializeField] private int _maxStack = 1;

        [SerializeField] private string _category;

        public ItemId Id => new ItemId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MaxStack => _maxStack;
        public string Category => _category;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[ItemDefinition] '{name}' has no Id assigned.", this);
            }
        }
#endif
    }
}
