using GameFramework.PlayerSystems;
using GameFramework.Runtime.Services;
using GameFramework.Tutorials;

namespace GameFramework.Samples.Phase9Demo
{
    /// <summary>
    /// Combines <see cref="PlayerSystemsBootstrapper"/> (for <c>IInputService</c>, needed by the
    /// demo's <see cref="Tutorials.Steps.InputStep"/>) with <see cref="ITutorialService"/> - exactly
    /// the small composition-root subclass <see cref="TutorialBootstrapper"/>'s own remarks document
    /// for combining Tutorials with any other sibling bootstrapper. Not part of the reusable
    /// framework - sample/demo content only.
    /// </summary>
    public sealed class Phase9DemoBootstrapper : PlayerSystemsBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<ITutorialService>(new TutorialService());
        }
    }
}
