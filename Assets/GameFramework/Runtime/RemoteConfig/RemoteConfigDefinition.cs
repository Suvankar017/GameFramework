using System;
using UnityEngine;

namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// One remotely-configurable key's local schema/default - a plain serializable entry inside
    /// <see cref="RemoteConfigConfiguration"/> rather than its own ScriptableObject asset (see
    /// <c>Monetization.Ads.AdPlacementConfig</c>'s remarks for the same reasoning).
    ///
    /// Declaring a definition is how a key becomes "remotely configurable" in the structured sense
    /// (typed, validated, range-checked) - see CLAUDE.md's Phase 17 brief, section 7: every value
    /// meant to be remotely configured must have a safe local default here. A game may still read an
    /// entirely undeclared, ad hoc key via <see cref="IRemoteConfigService"/>'s Get* methods (section
    /// 10); such a key only ever resolves from an actual cached/fetched value and otherwise falls
    /// back to the caller-supplied default rather than one authored here.
    /// </summary>
    [Serializable]
    public sealed class RemoteConfigDefinition
    {
        [Tooltip("Stable key, e.g. \"economy.reward_multiplier\". Never a localized display name.")]
        [SerializeField] private string _key;

        [SerializeField] private RemoteConfigTypedValue _defaultValue;

        [Tooltip("Informational grouping only (e.g. \"economy\", \"ads\") - not enforced by the framework. See CLAUDE.md's Phase 17 brief, section 57.")]
        [SerializeField] private string _domain;

        [SerializeField] private RuntimeChangePolicy _runtimeChangePolicy = RuntimeChangePolicy.SafeAtRuntime;

        [Tooltip("Numeric types only. A fetched/cached value outside [Min, Max] causes the whole snapshot to be rejected (see CLAUDE.md's Phase 17 brief, section 56).")]
        [SerializeField] private bool _hasRange;
        [SerializeField] private double _minValue;
        [SerializeField] private double _maxValue;

        public string Key => _key;
        public bool IsKeyValid => !string.IsNullOrEmpty(_key);
        public RemoteConfigValueType Type => _defaultValue.Type;
        public object DefaultValueBoxed => _defaultValue.BoxedValue;
        public string Domain => _domain;
        public RuntimeChangePolicy RuntimeChangePolicy => _runtimeChangePolicy;
        public bool HasRange => _hasRange;
        public double MinValue => _minValue;
        public double MaxValue => _maxValue;

#if UNITY_EDITOR
        internal void OnValidate()
        {
            if (_hasRange && _minValue > _maxValue)
            {
                (_minValue, _maxValue) = (_maxValue, _minValue);
            }
        }
#endif
    }
}
