using System;
using UnityEngine;

namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// One placement's authoring data - a plain serializable entry inside <see cref="AdConfiguration"/>
    /// rather than its own ScriptableObject asset, since (unlike <c>RewardDefinition</c>) nothing
    /// else in the framework needs to reference an individual placement by object identity (see
    /// CLAUDE.md rule 69, avoid unnecessary per-item assets).
    /// </summary>
    [Serializable]
    public sealed class AdPlacementConfig
    {
        [SerializeField] private string _id;
        [SerializeField] private AdType _type;
        [SerializeField] private string _androidUnitId;
        [SerializeField] private string _iosUnitId;

        [Tooltip("Minimum seconds between two successful shows of this placement. 0 = no cooldown.")]
        [SerializeField] private float _cooldownSeconds;

        [Tooltip("Maximum number of times this placement may be shown per application session. 0 = unlimited.")]
        [SerializeField] private int _sessionShowLimit;

        public AdPlacementId Id => new AdPlacementId(_id);
        public AdType Type => _type;
        public float CooldownSeconds => _cooldownSeconds;
        public int SessionShowLimit => _sessionShowLimit;

        /// <summary>Resolves the provider ad-unit id for the running platform - Android/iOS as
        /// authored, or empty in the Editor/any other platform (the mock provider does not need a
        /// real unit id).</summary>
        public string ResolvePlatformUnitId()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _androidUnitId;
#elif UNITY_IOS && !UNITY_EDITOR
            return _iosUnitId;
#else
            return string.Empty;
#endif
        }
    }
}
