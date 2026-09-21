using GameFramework.Cameras;
using GameFramework.Presentation;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Samples.Phase11Demo
{
    /// <summary>
    /// Registers exactly the same services as <see cref="Phase11DemoBootstrapper"/>
    /// (<see cref="ICameraService"/> + <see cref="IPresentationService"/>) - proof by construction
    /// that the Cinemachine integration adds no new service of its own. Everything
    /// Cinemachine-specific in this sample (<c>CinemachineCameraBackend</c>,
    /// <c>CinemachineCameraAdapter</c>) is plain scene composition, not a registered
    /// <see cref="IGameService"/>. Not part of the reusable framework - sample/demo content only.
    /// </summary>
    public sealed class Phase11CinemachineDemoBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<ICameraService>(new CameraService());
            registry.Register<IPresentationService>(new PresentationService());
        }
    }
}
