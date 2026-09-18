using GameFramework.Presentation.Configs;
using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Pure authoring data for one feedback bundle (e.g. "HeavyImpact", "Reward", "ButtonPress") -
    /// id/priority plus one optional config block per built-in <see cref="FeedbackChannel"/>. Never
    /// stores runtime state - the same asset is shared by every <see cref="IPresentationService.Play"/>
    /// call for its id, from every caller, for the whole application session.
    ///
    /// Deliberately a fixed set of typed fields rather than a polymorphic
    /// <c>[SerializeReference]</c> component list: the seven channels are a closed set CLAUDE.md's
    /// Phase 10 brief enumerates explicitly (section 8: "small, useful set... do not build dozens of
    /// specialized classes"), so there is no open-ended authoring need a fixed field list doesn't
    /// already cover - the same reasoning <see cref="Feedback.FeedbackPresetAsset"/> (Phase 3) already
    /// used for its own, smaller haptic+audio bundle.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Presentation/Feedback Definition", fileName = "FeedbackDefinition")]
    public sealed class FeedbackDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private FeedbackPriority _priority = FeedbackPriority.Normal;

        [SerializeField] private AudioFeedbackConfig _audio = new AudioFeedbackConfig();
        [SerializeField] private HapticFeedbackConfig _haptic = new HapticFeedbackConfig();
        [SerializeField] private CameraFeedbackConfig _camera = new CameraFeedbackConfig();
        [SerializeField] private VisualEffectFeedbackConfig _visual = new VisualEffectFeedbackConfig();
        [SerializeField] private ScreenEffectFeedbackConfig _screen = new ScreenEffectFeedbackConfig();
        [SerializeField] private UIFeedbackConfig _ui = new UIFeedbackConfig();
        [SerializeField] private TimeFeedbackConfig _time = new TimeFeedbackConfig();

        public FeedbackId Id => new FeedbackId(_id);
        public FeedbackPriority Priority => _priority;

        public AudioFeedbackConfig Audio => _audio;
        public HapticFeedbackConfig Haptic => _haptic;
        public CameraFeedbackConfig Camera => _camera;
        public VisualEffectFeedbackConfig Visual => _visual;
        public ScreenEffectFeedbackConfig Screen => _screen;
        public UIFeedbackConfig UI => _ui;
        public TimeFeedbackConfig Time => _time;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[FeedbackDefinition] '{name}' has no Id assigned.", this);
            }

            if (_audio.Enabled && _audio.Cue == null)
            {
                Debug.LogWarning($"[FeedbackDefinition] '{name}' has Audio enabled but no Cue assigned.", this);
            }

            if (_visual.Enabled && _visual.EffectPrefab == null)
            {
                Debug.LogWarning($"[FeedbackDefinition] '{name}' has Visual enabled but no EffectPrefab assigned.", this);
            }
        }
#endif
    }
}
