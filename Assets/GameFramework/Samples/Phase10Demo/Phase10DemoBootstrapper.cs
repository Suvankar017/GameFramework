using GameFramework.PlayerSystems;
using GameFramework.Presentation;
using GameFramework.Runtime.Services;

namespace GameFramework.Samples.Phase10Demo
{
    /// <summary>
    /// Combines <see cref="PlayerSystemsBootstrapper"/> (for <c>IAudioService</c>/<c>IFeedbackService</c>/
    /// <c>IUIService</c>, which most of the demo's feedback channels use) with
    /// <see cref="IPresentationService"/> - exactly the small composition-root subclass
    /// <see cref="PresentationBootstrapper"/>'s own remarks document for combining Presentation with
    /// any other sibling bootstrapper. Not part of the reusable framework - sample/demo content only.
    /// </summary>
    public sealed class Phase10DemoBootstrapper : PlayerSystemsBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<IPresentationService>(new PresentationService());
        }
    }
}
