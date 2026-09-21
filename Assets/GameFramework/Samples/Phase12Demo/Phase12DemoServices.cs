using GameFramework.Runtime.Bootstrap;
using GameFramework.UI.Navigation;

namespace GameFramework.Samples.Phase12Demo
{
    /// <summary>Sample-only convenience accessor - a real game typically resolves services through
    /// its own composition root / injected references rather than a static lookup like this.</summary>
    internal static class Phase12DemoServices
    {
        public static INavigationService Navigation =>
            GameBootstrapper.Instance != null && GameBootstrapper.Instance.State == BootstrapState.Ready
                ? GameBootstrapper.Instance.Services.Get<INavigationService>()
                : null;
    }
}
