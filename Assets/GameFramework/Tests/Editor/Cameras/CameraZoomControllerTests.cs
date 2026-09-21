using GameFramework.Cameras.Configuration;
using NUnit.Framework;

namespace GameFramework.Cameras.Tests
{
    public class CameraZoomControllerTests
    {
        private static CameraZoomSettings Settings() => new CameraZoomSettings
        {
            MinOrthographicSize = 2f,
            MaxOrthographicSize = 10f,
            DefaultOrthographicSize = 5f,
            ZoomDamping = 0f
        };

        [Test]
        public void SetZoom_WithinRange_NoDamping_TickReturnsExactValue()
        {
            var zoom = new CameraZoomController(Settings(), orthographic: true, initialValue: 5f);

            zoom.SetZoom(7f);
            float result = zoom.Tick(0.1f);

            Assert.AreEqual(7f, result, 0.0001f);
        }

        [Test]
        public void SetZoom_AboveMax_ClampsToMax()
        {
            var zoom = new CameraZoomController(Settings(), orthographic: true, initialValue: 5f);

            zoom.SetZoom(999f);
            float result = zoom.Tick(0.1f);

            Assert.AreEqual(10f, result, 0.0001f);
        }

        [Test]
        public void SetZoom_BelowMin_ClampsToMin()
        {
            var zoom = new CameraZoomController(Settings(), orthographic: true, initialValue: 5f);

            zoom.SetZoom(-99f);
            float result = zoom.Tick(0.1f);

            Assert.AreEqual(2f, result, 0.0001f);
        }

        [Test]
        public void Tick_WithDamping_MovesPartiallyTowardTarget()
        {
            var settings = Settings();
            settings.ZoomDamping = 1f;
            var zoom = new CameraZoomController(settings, orthographic: true, initialValue: 5f);

            zoom.SetZoom(10f);
            float result = zoom.Tick(0.02f);

            Assert.Greater(result, 5f);
            Assert.Less(result, 10f);
        }

        [Test]
        public void Tick_ReduceMotion_SkipsDampingEvenWhenConfigured()
        {
            var settings = Settings();
            settings.ZoomDamping = 5f;
            var zoom = new CameraZoomController(settings, orthographic: true, initialValue: 5f);

            zoom.SetZoom(9f);
            float result = zoom.Tick(0.02f, reduceMotion: true);

            Assert.AreEqual(9f, result, 0.0001f);
        }

        [Test]
        public void Snap_SetsCurrentToTargetImmediately_ClearingVelocity()
        {
            var settings = Settings();
            settings.ZoomDamping = 5f;
            var zoom = new CameraZoomController(settings, orthographic: true, initialValue: 5f);
            zoom.SetZoom(10f);
            zoom.Tick(0.02f); // start damping in progress

            zoom.Snap();

            Assert.AreEqual(10f, zoom.Current, 0.0001f);
        }
    }
}
