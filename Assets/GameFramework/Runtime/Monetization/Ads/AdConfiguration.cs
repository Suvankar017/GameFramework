using UnityEngine;

namespace GameFramework.Monetization.Ads
{
    /// <summary>
    /// Reusable, game-authored ad configuration - placements and the ad-type suppression rules
    /// entitlement ownership applies. See CLAUDE.md's Phase 15 brief, section 9: no ad-unit id ever
    /// appears in gameplay code, only here.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Monetization/Ad Configuration", fileName = "AdConfiguration")]
    public sealed class AdConfiguration : ScriptableObject
    {
        [SerializeField] private AdPlacementConfig[] _placements = System.Array.Empty<AdPlacementConfig>();
        [SerializeField] private AdEntitlementSuppressionRule[] _entitlementSuppressions = System.Array.Empty<AdEntitlementSuppressionRule>();

        [Tooltip("Maximum consecutive load retries after a load failure, before giving up until the next explicit Load() call.")]
        [SerializeField] private int _maxLoadRetries = 2;

        [Tooltip("Seconds to wait before each retry (multiplied by the retry attempt number).")]
        [SerializeField] private float _retryBackoffSeconds = 5f;

        public AdPlacementConfig[] Placements => _placements;
        public AdEntitlementSuppressionRule[] EntitlementSuppressions => _entitlementSuppressions;
        public int MaxLoadRetries => _maxLoadRetries;
        public float RetryBackoffSeconds => _retryBackoffSeconds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxLoadRetries < 0)
            {
                _maxLoadRetries = 0;
            }

            if (_retryBackoffSeconds < 0f)
            {
                _retryBackoffSeconds = 0f;
            }
        }
#endif
    }
}
