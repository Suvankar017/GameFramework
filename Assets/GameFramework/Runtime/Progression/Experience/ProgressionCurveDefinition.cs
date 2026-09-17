using UnityEngine;

namespace GameFramework.Progression.Experience
{
    /// <summary>
    /// Designer-authored <see cref="IProgressionCurve"/> — one asset type supporting both a linear
    /// formula and an explicit per-level table, rather than several ScriptableObject subclasses for
    /// each curve shape (simpler to author and swap in the Inspector). Pure authoring data; never
    /// mutated at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Progression/Progression Curve", fileName = "ProgressionCurveDefinition")]
    public sealed class ProgressionCurveDefinition : ScriptableObject, IProgressionCurve
    {
        [SerializeField] private ProgressionCurveMode _mode = ProgressionCurveMode.Linear;

        [Header("Linear")]
        [SerializeField] private int _baseExperience = 100;
        [SerializeField] private int _incrementPerLevel = 50;

        [Tooltip("0 = unlimited (Linear mode only - Table mode's length is always its effective max).")]
        [SerializeField] private int _maxLevel;

        [Header("Table")]
        [Tooltip("Table[0] = XP required from level 1 to level 2, Table[1] = level 2 to 3, etc.")]
        [SerializeField] private int[] _table = System.Array.Empty<int>();

        public int GetRequiredExperience(int level)
        {
            if (level < 1)
            {
                return 0;
            }

            if (_mode == ProgressionCurveMode.Table)
            {
                int index = level - 1;
                return index >= 0 && index < _table.Length ? Mathf.Max(0, _table[index]) : 0;
            }

            if (_maxLevel > 0 && level >= _maxLevel)
            {
                return 0;
            }

            return Mathf.Max(0, _baseExperience + (level - 1) * _incrementPerLevel);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_mode == ProgressionCurveMode.Table && _table.Length == 0)
            {
                Debug.LogWarning($"[ProgressionCurveDefinition] '{name}' is in Table mode with an empty table.", this);
            }
        }
#endif
    }
}
