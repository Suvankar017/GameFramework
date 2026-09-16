using GameFramework.Audio;
using GameFramework.Feedback;
using GameFramework.Input;
using GameFramework.Localization;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using GameFramework.UI;
using UnityEngine;

namespace GameFramework.PlayerSystems
{
    /// <summary>
    /// Drop-in <see cref="GameBootstrapper"/> that additionally registers Phase 3's five
    /// player-facing systems, in the dependency order Input → Localization → Audio → UI →
    /// Feedback (UI depends on Localization/Audio/Feedback already being initialized; nothing
    /// later depends on Input, Localization, or Audio being initialized before it, so their
    /// relative order only reflects the framework's own registration convention). This class is
    /// the framework's own composition root for Phase 3 — a game does not need to hand-register
    /// these five services itself, but is free to subclass this instead of
    /// <see cref="GameBootstrapper"/> directly to add game-specific services on top.
    /// </summary>
    public class PlayerSystemsBootstrapper : GameBootstrapper
    {
        [Header("UI")]
        [Tooltip("Screen Space - Overlay (default, zero setup) or Screen Space - Camera, for a 2D " +
                 "game where UI needs to share a camera stack with the world. See UICanvasConfig.")]
        [SerializeField] private UIRenderMode _uiRenderMode = UIRenderMode.ScreenSpaceOverlay;

        [Tooltip("Required when UI Render Mode is Screen Space - Camera; ignored otherwise.")]
        [SerializeField] private Camera _uiCamera;

        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IInputService>(new InputService());
            registry.Register<ILocalizationService>(new LocalizationService());
            registry.Register<IAudioService>(new AudioService());
            registry.Register<IUIService>(CreateUIService());
            registry.Register<IFeedbackService>(new FeedbackService());
        }

        /// <summary>Builds the <see cref="UIService"/> instance <see cref="RegisterServices"/>
        /// registers. Override to customize <see cref="UICanvasConfig"/> further (e.g. a
        /// non-default <see cref="UICanvasConfig.PlaneDistance"/>) instead of duplicating
        /// <see cref="RegisterServices"/> entirely.</summary>
        protected virtual UIService CreateUIService()
        {
            return new UIService(new UICanvasConfig
            {
                RenderMode = _uiRenderMode,
                WorldCamera = _uiCamera
            });
        }
    }
}
