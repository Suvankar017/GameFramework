using GameFramework.Cameras.Configuration;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Cameras.Tests
{
    public class CameraTransitionRunnerTests
    {
        private static CameraPose Pose(float x) => new CameraPose(new Vector3(x, 0f, 0f), Quaternion.identity, 5f, 60f);

        [Test]
        public void Begin_ZeroDuration_IsNotActive_TickReturnsTargetImmediately()
        {
            var runner = new CameraTransitionRunner();
            runner.Begin(Pose(0f), new CameraTransitionSettings { Duration = 0f });

            Assert.IsFalse(runner.IsActive);
            CameraPose result = runner.Tick(Pose(10f), 0.1f);
            Assert.AreEqual(10f, result.Position.x, 0.0001f);
        }

        [Test]
        public void Begin_NullSettings_TreatedAsImmediate()
        {
            var runner = new CameraTransitionRunner();
            runner.Begin(Pose(0f), null);

            Assert.IsFalse(runner.IsActive);
        }

        [Test]
        public void Tick_PartwayThroughDuration_BlendsBetweenFromAndTarget()
        {
            var runner = new CameraTransitionRunner();
            runner.Begin(Pose(0f), new CameraTransitionSettings { Duration = 1f });

            CameraPose result = runner.Tick(Pose(10f), 0.5f); // halfway, linear (no ease curve)

            Assert.AreEqual(5f, result.Position.x, 0.01f);
            Assert.IsTrue(runner.IsActive);
        }

        [Test]
        public void Tick_DurationFullyElapsed_ReturnsTargetExactly_AndBecomesInactive()
        {
            var runner = new CameraTransitionRunner();
            runner.Begin(Pose(0f), new CameraTransitionSettings { Duration = 1f });

            runner.Tick(Pose(10f), 0.6f);
            CameraPose result = runner.Tick(Pose(10f), 0.6f); // total elapsed now exceeds duration

            Assert.AreEqual(10f, result.Position.x, 0.0001f);
            Assert.IsFalse(runner.IsActive);
        }

        [Test]
        public void Cancel_StopsBlending_TickReturnsTargetDirectly()
        {
            var runner = new CameraTransitionRunner();
            runner.Begin(Pose(0f), new CameraTransitionSettings { Duration = 1f });

            runner.Cancel();
            CameraPose result = runner.Tick(Pose(10f), 0.1f);

            Assert.IsFalse(runner.IsActive);
            Assert.AreEqual(10f, result.Position.x, 0.0001f);
        }
    }
}
