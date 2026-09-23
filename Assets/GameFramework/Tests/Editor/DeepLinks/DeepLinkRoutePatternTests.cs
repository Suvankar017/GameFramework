using System.Collections.Generic;
using NUnit.Framework;

namespace GameFramework.DeepLinks.Tests
{
    public class DeepLinkRoutePatternTests
    {
        [Test]
        public void TryMatch_LiteralPattern_ExactPath_Matches()
        {
            var pattern = new DeepLinkRoutePattern("daily-reward");
            DeepLinkParser.TryParse("mygame://daily-reward", out DeepLink link);

            Assert.IsTrue(pattern.TryMatch(link, out IReadOnlyDictionary<string, string> parameters));
            Assert.AreEqual(0, parameters.Count);
        }

        [Test]
        public void TryMatch_LiteralPattern_DifferentPath_DoesNotMatch()
        {
            var pattern = new DeepLinkRoutePattern("daily-reward");
            DeepLinkParser.TryParse("mygame://weekly-reward", out DeepLink link);

            Assert.IsFalse(pattern.TryMatch(link, out _));
        }

        [Test]
        public void TryMatch_PathParameter_IsCaptured()
        {
            var pattern = new DeepLinkRoutePattern("shop/{itemId}");
            DeepLinkParser.TryParse("mygame://shop/sword-of-truth", out DeepLink link);

            Assert.IsTrue(pattern.TryMatch(link, out IReadOnlyDictionary<string, string> parameters));
            Assert.AreEqual("sword-of-truth", parameters["itemId"]);
        }

        [Test]
        public void TryMatch_SegmentCountMismatch_DoesNotMatch()
        {
            var pattern = new DeepLinkRoutePattern("shop/{itemId}");
            DeepLinkParser.TryParse("mygame://shop/sword/extra", out DeepLink link);

            Assert.IsFalse(pattern.TryMatch(link, out _));
        }

        [Test]
        public void TryMatch_CaseInsensitiveLiteralSegment()
        {
            var pattern = new DeepLinkRoutePattern("Daily-Reward");
            DeepLinkParser.TryParse("mygame://daily-reward", out DeepLink link);

            Assert.IsTrue(pattern.TryMatch(link, out _));
        }
    }
}
