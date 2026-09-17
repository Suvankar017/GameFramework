using UnityEngine;

namespace GameFramework.Progression.Statistics
{
    /// <summary>
    /// Pure authoring data for one tracked statistic — identity/type/bounds only, never mutated at
    /// runtime (see <see cref="StatisticsService"/> for where the mutable value actually lives).
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Progression/Statistic Definition", fileName = "StatisticDefinition")]
    public sealed class StatisticDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;
        [SerializeField] private StatisticValueType _valueType = StatisticValueType.Integer;

        [Tooltip("Integer-only. Rejects any operation that would lower the value (Decrement, or " +
            "Set to a smaller value) instead of applying it - use ResetToDefaults to override.")]
        [SerializeField] private bool _isMonotonic;

        [Tooltip("Integer-only. 0 = no minimum.")]
        [SerializeField] private int _minValue;

        [Tooltip("Integer-only. 0 = no maximum.")]
        [SerializeField] private int _maxValue;

        [Tooltip("If false, this statistic resets to its default every session and is never written " +
            "to the save file (e.g. a current-session counter rather than lifetime progression).")]
        [SerializeField] private bool _persistent = true;

        public StatisticId Id => new StatisticId(_id);
        public string DisplayName => _displayName;
        public string Description => _description;
        public StatisticValueType ValueType => _valueType;
        public bool IsMonotonic => _isMonotonic;
        public int MinValue => _minValue;
        public int MaxValue => _maxValue;
        public bool Persistent => _persistent;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[StatisticDefinition] '{name}' has no Id assigned.", this);
            }

            if (_maxValue > 0 && _minValue > _maxValue)
            {
                Debug.LogWarning($"[StatisticDefinition] '{name}' has MinValue greater than MaxValue.", this);
            }
        }
#endif
    }
}
