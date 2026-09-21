using GameFramework.Cameras.CinemachineIntegration;
using GameFramework.Cameras.Configuration;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.CinemachineIntegration.Tests
{
    /// <summary>Covers the one genuinely new piece of math this integration adds - translating a
    /// <see cref="CameraBoundsSettings"/> rect into the center/size a <c>BoxCollider2D</c> needs for
    /// <c>CinemachineConfiner2D</c>. Pure, so fully EditMode-testable without a live scene.</summary>
    public class CinemachineBoundsTranslatorTests
    {
        [Test]
        public void TryComputeBoxBounds_Disabled_ReturnsFalse()
        {
            var bounds = new CameraBoundsSettings { Enabled = false, MinX = -5f, MaxX = 5f, MinY = -5f, MaxY = 5f };

            bool result = CinemachineBoundsTranslator.TryComputeBoxBounds(bounds, out _, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryComputeBoxBounds_Null_ReturnsFalse()
        {
            bool result = CinemachineBoundsTranslator.TryComputeBoxBounds(null, out _, out _);

            Assert.IsFalse(result);
        }

        [TestCase(5f, -5f)] // inverted (Min > Max) -> zero/negative width
        [TestCase(5f, 5f)]  // degenerate (Min == Max) -> zero width
        public void TryComputeBoxBounds_DegenerateWidth_ReturnsFalse(float minX, float maxX)
        {
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = minX, MaxX = maxX, MinY = -5f, MaxY = 5f };

            bool result = CinemachineBoundsTranslator.TryComputeBoxBounds(bounds, out _, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryComputeBoxBounds_Valid_ReturnsCenterAndSize()
        {
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -10f, MaxX = 20f, MinY = -4f, MaxY = 6f };

            bool result = CinemachineBoundsTranslator.TryComputeBoxBounds(bounds, out Vector2 center, out Vector2 size);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2(5f, 1f), center);
            Assert.AreEqual(new Vector2(30f, 10f), size);
        }
    }
}
