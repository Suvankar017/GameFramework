using UnityEngine;

namespace GameFramework.Analytics
{
    /// <summary>
    /// Reusable, game-authored analytics configuration - see CLAUDE.md's Phase 16 brief, section 52.
    /// Every limit here is a validation/sanitization boundary <see cref="AnalyticsService"/> enforces
    /// before an event or parameter ever reaches a provider; a provider adapter may still apply its
    /// own, stricter rules on top (section 10).
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Analytics/Analytics Configuration", fileName = "AnalyticsConfiguration")]
    public sealed class AnalyticsConfiguration : ScriptableObject
    {
        [SerializeField] private bool _enabledByDefault = true;
        [SerializeField] private bool _consentRequired = true;
        [SerializeField] private ConsentPolicy _consentPolicy = ConsentPolicy.BufferUntilDecided;
        [SerializeField] private AnalyticsEnvironment _environment = AnalyticsEnvironment.Development;

        [Tooltip("Maximum number of events held while consent is Unknown and ConsentPolicy is BufferUntilDecided. Oldest event is dropped once full.")]
        [SerializeField] private int _eventQueueCapacity = 100;

        [Tooltip("Parameters beyond this count on a single Track() call are dropped (a single warning is logged, not one per parameter).")]
        [SerializeField] private int _maxParametersPerEvent = 25;

        [SerializeField] private int _maxEventNameLength = 40;
        [SerializeField] private int _maxParameterKeyLength = 40;

        [Tooltip("A string parameter value longer than this is truncated, not dropped.")]
        [SerializeField] private int _maxParameterValueLength = 100;

        [Tooltip("How long the app must stay backgrounded before the next resume starts a new analytics session instead of continuing the current one.")]
        [SerializeField] private float _sessionTimeoutSeconds = 1800f;

        public bool EnabledByDefault => _enabledByDefault;
        public bool ConsentRequired => _consentRequired;
        public ConsentPolicy ConsentPolicy => _consentPolicy;
        public AnalyticsEnvironment Environment => _environment;
        public int EventQueueCapacity => _eventQueueCapacity;
        public int MaxParametersPerEvent => _maxParametersPerEvent;
        public int MaxEventNameLength => _maxEventNameLength;
        public int MaxParameterKeyLength => _maxParameterKeyLength;
        public int MaxParameterValueLength => _maxParameterValueLength;
        public float SessionTimeoutSeconds => _sessionTimeoutSeconds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_eventQueueCapacity < 1) _eventQueueCapacity = 1;
            if (_maxParametersPerEvent < 1) _maxParametersPerEvent = 1;
            if (_maxEventNameLength < 1) _maxEventNameLength = 1;
            if (_maxParameterKeyLength < 1) _maxParameterKeyLength = 1;
            if (_maxParameterValueLength < 1) _maxParameterValueLength = 1;
            if (_sessionTimeoutSeconds < 0f) _sessionTimeoutSeconds = 0f;
        }
#endif
    }
}
