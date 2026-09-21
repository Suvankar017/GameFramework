using GameFramework.Cameras.Modes;
using NUnit.Framework;

namespace GameFramework.Cameras.Tests
{
    public class CameraDeadZoneTests
    {
        [Test]
        public void ClampAxis_OffsetInsideDeadZone_ReturnsZero()
        {
            float move = CameraDeadZone.ClampAxis(offset: 2f, deadZoneHalf: 3f, softZoneHalf: 0f);
            Assert.AreEqual(0f, move);
        }

        [Test]
        public void ClampAxis_OffsetAtDeadZoneEdge_ReturnsZero()
        {
            float move = CameraDeadZone.ClampAxis(offset: 3f, deadZoneHalf: 3f, softZoneHalf: 0f);
            Assert.AreEqual(0f, move);
        }

        [Test]
        public void ClampAxis_OffsetBeyondDeadZone_NoSoftZone_ReturnsExactExcess()
        {
            float move = CameraDeadZone.ClampAxis(offset: 5f, deadZoneHalf: 3f, softZoneHalf: 0f);
            Assert.AreEqual(2f, move, 0.0001f);
        }

        [Test]
        public void ClampAxis_NegativeOffsetBeyondDeadZone_ReturnsNegativeExcess()
        {
            float move = CameraDeadZone.ClampAxis(offset: -5f, deadZoneHalf: 3f, softZoneHalf: 0f);
            Assert.AreEqual(-2f, move, 0.0001f);
        }

        [Test]
        public void ClampAxis_WithinSoftZone_ReturnsPartialMove_LessThanFullExcess()
        {
            // deadZoneHalf=3, softZoneHalf=4: at offset=5, excess=2, fully inside the 4-unit soft band.
            float move = CameraDeadZone.ClampAxis(offset: 5f, deadZoneHalf: 3f, softZoneHalf: 4f);
            Assert.Greater(move, 0f);
            Assert.Less(move, 2f); // eased, so strictly less than the un-eased full excess
        }

        [Test]
        public void ClampAxis_BeyondSoftZone_ReturnsFullExcess()
        {
            // deadZoneHalf=3, softZoneHalf=1: at offset=10, excess=7, far beyond the 1-unit soft band.
            float move = CameraDeadZone.ClampAxis(offset: 10f, deadZoneHalf: 3f, softZoneHalf: 1f);
            Assert.AreEqual(7f, move, 0.0001f);
        }
    }
}
