using System;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Presentation
{
    /// <summary>
    /// Coordinated game-feel/presentation orchestration - Phase 10's equivalent of
    /// <see cref="GameFlow.IGameFlowService"/>/<see cref="Tutorials.ITutorialService"/>: one narrow,
    /// registered service gameplay code calls to say "something important happened," without
    /// knowing how each channel (audio/haptics/camera/visual/screen/UI/time) presents it.
    ///
    /// This is explicitly <b>not</b> a replacement for Phase 3's <see cref="Feedback.IFeedbackService"/>/
    /// <see cref="Audio.IAudioService"/> - it orchestrates them (see <see cref="Play"/>'s remarks),
    /// never duplicates their playback/haptic logic.
    /// </summary>
    public interface IPresentationService : IGameService
    {
        /// <summary>Registers one feedback bundle's authoring data. Call once per
        /// <see cref="FeedbackId"/> at composition-root time. Throws <see cref="ArgumentException"/>
        /// for a definition with no Id, and <see cref="InvalidOperationException"/> for a
        /// duplicate Id - both authoring/programmer errors caught once at startup.</summary>
        void RegisterDefinition(FeedbackDefinition definition);

        bool IsRegistered(FeedbackId id);

        /// <summary>The registered authoring data for <paramref name="id"/>, or null if it was
        /// never registered.</summary>
        FeedbackDefinition GetDefinition(FeedbackId id);

        /// <summary>
        /// Executes every enabled, currently-allowed channel on <paramref name="id"/>'s
        /// <see cref="FeedbackDefinition"/>: <see cref="Configs.AudioFeedbackConfig"/> plays through
        /// the existing <see cref="Audio.IAudioService"/>, <see cref="Configs.HapticFeedbackConfig"/>
        /// through the existing <see cref="Feedback.IFeedbackService"/>,
        /// <see cref="Configs.CameraFeedbackConfig"/> is forwarded to the currently registered
        /// <see cref="ICameraFeedbackDriver"/> (a no-op if none is), <see cref="Configs.VisualEffectFeedbackConfig"/>
        /// spawns through the existing pooling, <see cref="Configs.ScreenEffectFeedbackConfig"/>
        /// renders through the existing UI layer roots, <see cref="Configs.UIFeedbackConfig"/>
        /// publishes <see cref="UIFeedbackRequestedEvent"/>, and <see cref="Configs.TimeFeedbackConfig"/>
        /// applies a temporary <see cref="Runtime.Time.ITimeService"/> time-scale impulse.
        ///
        /// Composable channels (Audio/Haptics/Visual/UI/Camera-shake) always execute if enabled;
        /// exclusive channels (Screen/Time) only interrupt an already-playing effect on that same
        /// channel if their priority is greater than or equal to it - see
        /// <see cref="PresentationService"/>'s remarks.
        ///
        /// Always publishes <see cref="FeedbackPlayedEvent"/> with the returned result.
        /// </summary>
        PlayResult Play(FeedbackId id, Vector3? worldPosition = null, UnityEngine.Object source = null, float intensity = 1f);

        /// <summary>
        /// Subscribes <paramref name="id"/>'s feedback to fire automatically whenever
        /// <typeparamref name="TEvent"/> is published through the existing
        /// <see cref="Runtime.Events.IEventService"/> - the explicit, strongly-typed mapping
        /// CLAUDE.md's Phase 10 brief (section 27) asks for, never reflection/string-based
        /// discovery. <paramref name="requestFactory"/>, if supplied, builds the actual
        /// <see cref="FeedbackRequest"/> (e.g. the event's own world position) from the event
        /// payload; omitted, a plain request with no position/source and full intensity is used.
        /// Throws <see cref="InvalidOperationException"/> if <typeparamref name="TEvent"/> already
        /// has a registered mapping.
        /// </summary>
        void RegisterMapping<TEvent>(FeedbackId id, Func<TEvent, FeedbackRequest> requestFactory = null);

        /// <summary>Removes a mapping registered via <see cref="RegisterMapping{TEvent}"/>. A no-op
        /// if <typeparamref name="TEvent"/> has none.</summary>
        void UnregisterMapping<TEvent>();

        /// <summary>Registers the driver <see cref="Configs.CameraFeedbackConfig"/> requests are
        /// forwarded to. The most recently registered driver wins; typically called from
        /// <see cref="CameraFeedbackDriver"/>'s own <c>OnEnable</c>.</summary>
        void RegisterCameraDriver(ICameraFeedbackDriver driver);

        /// <summary>Unregisters <paramref name="driver"/> - a no-op if it is not the currently
        /// registered driver (so a stale <c>OnDisable</c> from an old scene's driver can never clear
        /// a legitimately active new one during a scene transition).</summary>
        void UnregisterCameraDriver(ICameraFeedbackDriver driver);
    }
}
