using GameFramework.Runtime.Security;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameFramework.Runtime.Tests.Security
{
    public class DevelopmentProviderGuardTests
    {
        private interface IProvider
        {
        }

        private sealed class MockProvider : IProvider
        {
        }

        private sealed class FallbackProvider : IProvider
        {
        }

        [Test]
        public void Select_MockRequestedInDevelopmentBuild_ReturnsMock()
        {
            IProvider provider = DevelopmentProviderGuard.Select<IProvider>(true, true, () => new MockProvider(), () => new FallbackProvider(), "Test");

            Assert.IsInstanceOf<MockProvider>(provider);
        }

        [Test]
        public void Select_MockRequestedInReleaseBuild_ReturnsFallbackAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("development-only \\(mock\\) provider"));

            IProvider provider = DevelopmentProviderGuard.Select<IProvider>(true, false, () => new MockProvider(), () => new FallbackProvider(), "Test");

            Assert.IsInstanceOf<FallbackProvider>(provider);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Select_MockNotRequested_AlwaysReturnsFallback(bool isDevelopmentBuild)
        {
            IProvider provider = DevelopmentProviderGuard.Select<IProvider>(false, isDevelopmentBuild, () => new MockProvider(), () => new FallbackProvider(), "Test");

            Assert.IsInstanceOf<FallbackProvider>(provider);
        }

        [Test]
        public void BuildEnvironment_InEditor_IsDevelopment()
        {
            Assert.IsTrue(BuildEnvironment.IsEditor);
            Assert.IsTrue(BuildEnvironment.IsDevelopmentBuild);
        }
    }
}
