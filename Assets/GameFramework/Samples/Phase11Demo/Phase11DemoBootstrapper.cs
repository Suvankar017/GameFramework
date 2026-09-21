using GameFramework.Cameras;
using GameFramework.Presentation;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;

namespace GameFramework.Samples.Phase11Demo
{
    /// <summary>
    /// Registers Phase 11's <see cref="ICameraService"/> alongside Phase 10's
    /// <see cref="IPresentationService"/> (for the camera-feedback/shake step of the sample
    /// pipeline) - a plain <see cref="GameBootstrapper"/> subclass rather than
    /// <c>PlayerSystemsBootstrapper</c>, since this sample deliberately only exercises the Camera
    /// channel of Presentation and needs none of Audio/UI/Haptics/Localization/Input. Not part of
    /// the reusable framework - sample/demo content only.
    /// </summary>
    public sealed class Phase11DemoBootstrapper : GameBootstrapper
    {
        protected override void RegisterServices(IServiceRegistry registry)
        {
            base.RegisterServices(registry);

            registry.Register<ICameraService>(new CameraService());
            registry.Register<IPresentationService>(new PresentationService());
        }
    }
}
