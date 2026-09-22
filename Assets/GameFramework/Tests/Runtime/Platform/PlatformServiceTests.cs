using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.Platform.Tests
{
    /// <summary><see cref="PlatformService.Initialize"/> only reads
    /// <see cref="UnityEngine.Application.platform"/>/<see cref="UnityEngine.Application.isEditor"/>
    /// and never calls <see cref="IServiceRegistry.Get{TService}"/>, so this can use a plain,
    /// never-marked-initialized registry directly rather than a full bootstrapper.</summary>
    public class PlatformServiceTests
    {
        private PlatformService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new PlatformService();
            _service.Initialize(new ServiceRegistry());
        }

        [Test]
        public void InEditor_ReportsEditorPlatform()
        {
            Assert.AreEqual(PlatformType.Editor, _service.Platform);
            Assert.IsTrue(_service.IsEditor);
        }

        [Test]
        public void InEditor_IsNotMobileAndNotDesktop()
        {
            // Editor is deliberately its own PlatformType, distinct from IsMobile/IsDesktop - see
            // IPlatformService's remarks.
            Assert.IsFalse(_service.IsMobile);
            Assert.IsFalse(_service.IsAndroid);
            Assert.IsFalse(_service.IsIOS);
            Assert.IsFalse(_service.IsDesktop);
        }
    }
}
