using System.Collections;
using GameFramework.Presentation;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.Runtime.Settings;
using UnityEngine;

namespace GameFramework.Samples.Phase10Demo
{
    /// <summary>
    /// Drives the Phase 10 demonstration scene: registers two "FeedbackDefinition" content assets
    /// under <c>Content/</c> and plays them on key press, showing Haptics/Camera/Screen/Time/Visual/
    /// UI channels working together from one <see cref="IPresentationService.Play"/> call - the
    /// framework's own <see cref="PresentationBootstrapper"/> only registers the service (see this
    /// class for the game-specific step of registering actual content, done here after Ready, the
    /// same pattern every other phase's demo controller follows). The Audio channel is left disabled
    /// on both demo definitions - wiring it up needs an authored <c>AudioCueAsset</c>+<c>AudioClip</c>,
    /// exactly like any other Phase 3 audio content, which this generic sample does not ship its own
    /// copy of. Not part of the reusable framework - sample/demo content only.
    /// </summary>
    public sealed class Phase10DemoController : MonoBehaviour
    {
        private static readonly FeedbackId HeavyImpact = new FeedbackId("HeavyImpact");
        private static readonly FeedbackId Reward = new FeedbackId("Reward");

        [Tooltip("Assigned from Content/HeavyImpact.asset.")]
        [SerializeField] private FeedbackDefinition _heavyImpact;

        [Tooltip("Assigned from Content/Reward.asset.")]
        [SerializeField] private FeedbackDefinition _reward;

        private IPresentationService _presentation;
        private ISettingsService _settings;
        private bool _ready;

        private IEnumerator Start()
        {
            while (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                yield return null;
            }

            IServiceRegistry services = GameBootstrapper.Instance.Services;
            _presentation = services.Get<IPresentationService>();
            _settings = services.Get<ISettingsService>();
            IEventService events = services.Get<IEventService>();

            _presentation.RegisterDefinition(_heavyImpact);
            _presentation.RegisterDefinition(_reward);

            events.Subscribe<FeedbackPlayedEvent>(e => Debug.Log($"[Phase10Demo] FeedbackPlayed: {e.Id} -> {e.Result}"));
            events.Subscribe<UIFeedbackRequestedEvent>(e => Debug.Log($"[Phase10Demo] UIFeedbackRequested: {e.Id} (Tag='{e.Tag}')"));

            _ready = true;
            Debug.Log("[Phase10Demo] Ready. Keys: 1 = play HeavyImpact (Haptic+Camera+Screen+Time+Visual+UI), " +
                "2 = play Reward (Haptic+UI), M = toggle master Presentation.Enabled, " +
                "K = toggle the Camera channel, R = status report.");
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                PlayResult result = _presentation.Play(HeavyImpact, worldPosition: transform.position);
                Debug.Log($"[Phase10Demo] Play(HeavyImpact) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                PlayResult result = _presentation.Play(Reward);
                Debug.Log($"[Phase10Demo] Play(Reward) -> {result}");
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                bool enabled = !_settings.Get<bool>("Presentation.Enabled");
                _settings.Set("Presentation.Enabled", enabled);
                Debug.Log($"[Phase10Demo] Presentation.Enabled -> {enabled}");
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                bool enabled = !_settings.Get<bool>("Presentation.Channel.Camera");
                _settings.Set("Presentation.Channel.Camera", enabled);
                Debug.Log($"[Phase10Demo] Presentation.Channel.Camera -> {enabled}");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log($"[Phase10Demo] Status: Enabled={_settings.Get<bool>("Presentation.Enabled")}, " +
                    $"Camera={_settings.Get<bool>("Presentation.Channel.Camera")}, " +
                    $"IntensityScale={_settings.Get<float>("Presentation.IntensityScale")}.");
            }
        }
    }
}
