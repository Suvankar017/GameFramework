using UnityEngine;

namespace GameFramework.Progression.Economy
{
    /// <summary>
    /// Pure authoring data for one currency — identity/display/bounds only, never mutated at
    /// runtime (see <see cref="EconomyService"/> for where the mutable balance actually lives).
    /// Shared across every player using this content, so it must never hold per-player state.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Progression/Currency Definition", fileName = "CurrencyDefinition")]
    public sealed class CurrencyDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        [Tooltip("0 = no maximum.")]
        [SerializeField] private int _maxBalance;

        [SerializeField] private int _minBalance;

        public CurrencyId Id => new CurrencyId(_id);
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public int MaxBalance => _maxBalance;
        public int MinBalance => _minBalance;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[CurrencyDefinition] '{name}' has no Id assigned.", this);
            }

            if (_maxBalance > 0 && _minBalance > _maxBalance)
            {
                Debug.LogWarning($"[CurrencyDefinition] '{name}' has MinBalance greater than MaxBalance.", this);
            }
        }
#endif
    }
}
