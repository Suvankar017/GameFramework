using GameFramework.Audio;
using GameFramework.Feedback;
using GameFramework.Input;
using GameFramework.Localization;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using GameFramework.UI;

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
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IInputService>(new InputService());
            registry.Register<ILocalizationService>(new LocalizationService());
            registry.Register<IAudioService>(new AudioService());
            registry.Register<IUIService>(new UIService());
            registry.Register<IFeedbackService>(new FeedbackService());
        }
    }
}
