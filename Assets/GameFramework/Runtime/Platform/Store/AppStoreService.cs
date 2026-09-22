using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;

namespace GameFramework.Platform
{
    /// <summary>
    /// Default <see cref="IAppStoreService"/>. Builds the platform-appropriate store URL and opens
    /// it through <see cref="IPlatformUrlService"/> - no native code is required for this, since
    /// both the Play Store and App Store expose ordinary URL schemes (see CLAUDE.md's Phase 14
    /// brief, section 20).
    /// </summary>
    public sealed class AppStoreService : IAppStoreService
    {
        private readonly AppStoreConfig _config;
        private IPlatformService _platform;
        private IPlatformUrlService _urlService;

        public AppStoreService(AppStoreConfig config)
        {
            _config = config;
        }

        public bool HasConfiguration =>
            _config != null &&
            (!string.IsNullOrEmpty(_config.AndroidPackageName) || !string.IsNullOrEmpty(_config.IOSAppStoreId));

        public void Initialize(IServiceRegistry registry)
        {
            _platform = registry.Get<IPlatformService>();
            _urlService = registry.Get<IPlatformUrlService>();
        }

        public void Shutdown()
        {
        }

        public bool OpenStorePage() => OpenUrlOrWarn(BuildStoreUrl(_platform.Platform, _config, reviewMode: false));

        public bool OpenReviewPage() => OpenUrlOrWarn(BuildStoreUrl(_platform.Platform, _config, reviewMode: true));

        private bool OpenUrlOrWarn(string url)
        {
            if (url == null)
            {
                Log.Warning("Platform", "No app store configuration for the current platform.");
                return false;
            }

            return _urlService.OpenUrl(url);
        }

        /// <summary>Public and pure (no <see cref="UnityEngine.Application.OpenURL"/> side effect)
        /// so the URL construction itself is directly testable. Returns null when
        /// <paramref name="platform"/> has no store surface (Editor/desktop/unknown) or
        /// <paramref name="config"/> has no identifier for it.</summary>
        public static string BuildStoreUrl(PlatformType platform, AppStoreConfig config, bool reviewMode)
        {
            if (config == null)
            {
                return null;
            }

            switch (platform)
            {
                case PlatformType.Android:
                    if (string.IsNullOrEmpty(config.AndroidPackageName))
                    {
                        return null;
                    }

                    return reviewMode
                        ? $"market://details?id={config.AndroidPackageName}&showAllReviews=true"
                        : $"market://details?id={config.AndroidPackageName}";

                case PlatformType.IOS:
                    if (string.IsNullOrEmpty(config.IOSAppStoreId))
                    {
                        return null;
                    }

                    return reviewMode
                        ? $"itms-apps://itunes.apple.com/app/id{config.IOSAppStoreId}?action=write-review"
                        : $"itms-apps://itunes.apple.com/app/id{config.IOSAppStoreId}";

                default:
                    return null;
            }
        }
    }
}
