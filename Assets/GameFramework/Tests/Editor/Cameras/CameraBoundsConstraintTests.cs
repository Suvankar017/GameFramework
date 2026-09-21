using GameFramework.Cameras.Configuration;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class CameraBoundsConstraintTests
    {
        [Test]
        public void Clamp_Disabled_ReturnsPositionUnchanged()
        {
            var bounds = new CameraBoundsSettings { Enabled = false, MinX = -1f, MaxX = 1f, MinY = -1f, MaxY = 1f };

            Vector3 result = CameraBoundsConstraint.Clamp(new Vector3(100f, 100f, 0f), bounds, orthographic: true, orthographicSize: 5f, aspect: 1f);

            Assert.AreEqual(new Vector3(100f, 100f, 0f), result);
        }

        [Test]
        public void Clamp_PositionInsideBounds_Unchanged()
        {
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -20f, MaxX = 20f, MinY = -20f, MaxY = 20f };

            Vector3 result = CameraBoundsConstraint.Clamp(new Vector3(1f, -1f, 3f), bounds, orthographic: true, orthographicSize: 5f, aspect: 1f);

            Assert.AreEqual(new Vector3(1f, -1f, 3f), result);
        }

        [Test]
        public void Clamp_Orthographic_AccountsForHalfExtents()
        {
            // size=5, aspect=1 => halfWidth=5, halfHeight=5. Bounds [-10,10] => valid center range [-5,5].
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -10f, MaxX = 10f, MinY = -10f, MaxY = 10f };

            Vector3 result = CameraBoundsConstraint.Clamp(new Vector3(9f, 0f, 0f), bounds, orthographic: true, orthographicSize: 5f, aspect: 1f);

            Assert.AreEqual(5f, result.x, 0.0001f);
        }

        [Test]
        public void Clamp_BoundsNarrowerThanCameraExtent_CentersInsteadOfInvertedClamp()
        {
            // size=10 => halfWidth=10, but bounds only span 4 total => adjustedMin > adjustedMax.
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -2f, MaxX = 2f, MinY = -2f, MaxY = 2f };

            Vector3 result = CameraBoundsConstraint.Clamp(new Vector3(50f, -50f, 0f), bounds, orthographic: true, orthographicSize: 10f, aspect: 1f);

            Assert.AreEqual(0f, result.x, 0.0001f); // (MinX+MaxX)/2
            Assert.AreEqual(0f, result.y, 0.0001f);
        }

        [Test]
        public void Clamp_Perspective_ClampsCenterOnly_NoExtentCompensation()
        {
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -5f, MaxX = 5f, MinY = -5f, MaxY = 5f };

            Vector3 result = CameraBoundsConstraint.Clamp(new Vector3(10f, 0f, 0f), bounds, orthographic: false, orthographicSize: 5f, aspect: 1f);

            Assert.AreEqual(5f, result.x, 0.0001f); // clamped straight to MaxX, no half-extent subtracted
        }

        [Test]
        public void Clamp_ZAxis_NeverClamped()
        {
            var bounds = new CameraBoundsSettings { Enabled = true, MinX = -1f, MaxX = 1f, MinY = -1f, MaxY = 1f };

            Vector3 result = CameraBoundsConstraint.Clamp(new Vector3(0f, 0f, -999f), bounds, orthographic: true, orthographicSize: 0.1f, aspect: 1f);

            Assert.AreEqual(-999f, result.z);
        }
    }
}
