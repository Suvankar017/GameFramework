using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>
    /// Default <see cref="IAppSettingsService"/>. Android opens the app's details screen via the
    /// one isolated native call in this framework
    /// (<see cref="Android.AndroidAppSettingsProvider"/>); Unity exposes no public, non-deprecated
    /// API for this on iOS without a native bridge, so iOS/Editor/desktop honestly report
    /// unsupported rather than guessing at an undocumented URL scheme (see CLAUDE.md's Phase 14
    /// brief, section 26).
    /// </summary>
    public sealed class AppSettingsService : IAppSettingsService
    {
        private IPlatformService _platform;

        public void Initialize(IServiceRegistry registry)
        {
            _platform = registry.Get<IPlatformService>();
        }

        public void Shutdown()
        {
        }

        public bool OpenApplicationSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_platform.IsAndroid)
            {
                return Android.AndroidAppSettingsProvider.OpenApplicationDetailsSettings();
            }
#endif
            Log.Warning("Platform", $"OpenApplicationSettings is not supported on {_platform.Platform}.");
            return false;
        }
    }
}
