using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Platform.Tests
{
    public class AppStoreServiceTests
    {
        private AppStoreConfig CreateConfig(string androidPackage, string iosId)
        {
            var config = ScriptableObject.CreateInstance<AppStoreConfig>();
            config.AndroidPackageName = androidPackage;
            config.IOSAppStoreId = iosId;
            return config;
        }

        [Test]
        public void BuildStoreUrl_NullConfig_ReturnsNull()
        {
            Assert.IsNull(AppStoreService.BuildStoreUrl(PlatformType.Android, null, reviewMode: false));
        }

        [Test]
        public void BuildStoreUrl_Android_BuildsMarketUrl()
        {
            AppStoreConfig config = CreateConfig("com.company.game", null);

            string url = AppStoreService.BuildStoreUrl(PlatformType.Android, config, reviewMode: false);

            Assert.AreEqual("market://details?id=com.company.game", url);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildStoreUrl_AndroidReviewMode_IncludesShowAllReviews()
        {
            AppStoreConfig config = CreateConfig("com.company.game", null);

            string url = AppStoreService.BuildStoreUrl(PlatformType.Android, config, reviewMode: true);

            Assert.AreEqual("market://details?id=com.company.game&showAllReviews=true", url);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildStoreUrl_Ios_BuildsItunesUrl()
        {
            AppStoreConfig config = CreateConfig(null, "123456789");

            string url = AppStoreService.BuildStoreUrl(PlatformType.IOS, config, reviewMode: false);

            Assert.AreEqual("itms-apps://itunes.apple.com/app/id123456789", url);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildStoreUrl_MissingIdentifierForPlatform_ReturnsNull()
        {
            AppStoreConfig config = CreateConfig(null, "123456789");

            string url = AppStoreService.BuildStoreUrl(PlatformType.Android, config, reviewMode: false);

            Assert.IsNull(url);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildStoreUrl_EditorOrDesktopPlatform_ReturnsNull()
        {
            AppStoreConfig config = CreateConfig("com.company.game", "123456789");

            Assert.IsNull(AppStoreService.BuildStoreUrl(PlatformType.Editor, config, reviewMode: false));
            Assert.IsNull(AppStoreService.BuildStoreUrl(PlatformType.Windows, config, reviewMode: false));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void HasConfiguration_NoConfig_IsFalse()
        {
            var service = new AppStoreService(null);

            Assert.IsFalse(service.HasConfiguration);
        }

        [Test]
        public void HasConfiguration_EmptyConfig_IsFalse()
        {
            AppStoreConfig config = CreateConfig(null, null);
            var service = new AppStoreService(config);

            Assert.IsFalse(service.HasConfiguration);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void HasConfiguration_WithAndroidPackage_IsTrue()
        {
            AppStoreConfig config = CreateConfig("com.company.game", null);
            var service = new AppStoreService(config);

            Assert.IsTrue(service.HasConfiguration);
            Object.DestroyImmediate(config);
        }
    }
}
