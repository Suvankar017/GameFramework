using GameFramework.Presentation.Configs;
using NUnit.Framework;

namespace GameFramework.Presentation.Tests
{
    public class TimeFeedbackControllerTests
    {
        private static TimeFeedbackConfig Config(float duration, float timeScale = 0.1f) =>
            new TimeFeedbackConfig { Duration = duration, TimeScale = timeScale };

        [Test]
        public void TryPlay_Activates()
        {
            var controller = new TimeFeedbackController();

            bool accepted = controller.TryPlay(Config(0.1f), FeedbackPriority.Normal);

            Assert.IsTrue(accepted);
            Assert.IsTrue(controller.IsActive);
        }

        [Test]
        public void Tick_BeforeDurationElapses_ReturnsFalse_StaysActive()
        {
            var controller = new TimeFeedbackController();
            controller.TryPlay(Config(1f), FeedbackPriority.Normal);

            bool finished = controller.Tick(0.5f);

            Assert.IsFalse(finished);
            Assert.IsTrue(controller.IsActive);
        }

        [Test]
        public void Tick_PastDuration_ReturnsTrueExactlyOnce_ThenInactive()
        {
            var controller = new TimeFeedbackController();
            controller.TryPlay(Config(0.2f), FeedbackPriority.Normal);

            bool first = controller.Tick(0.25f);
            bool second = controller.Tick(0.1f);

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.IsFalse(controller.IsActive);
        }

        [Test]
        public void TryPlay_LowerPriorityWhileActive_IsRejected()
        {
            var controller = new TimeFeedbackController();
            controller.TryPlay(Config(1f), FeedbackPriority.High);

            bool accepted = controller.TryPlay(Config(1f), FeedbackPriority.Low);

            Assert.IsFalse(accepted);
        }

        [Test]
        public void TryPlay_EqualOrHigherPriorityWhileActive_Replaces_ResettingRemainingDuration()
        {
            var controller = new TimeFeedbackController();
            controller.TryPlay(Config(0.1f), FeedbackPriority.Normal);
            controller.Tick(0.09f); // nearly finished

            bool accepted = controller.TryPlay(Config(1f), FeedbackPriority.Normal);

            Assert.IsTrue(accepted);
            Assert.IsFalse(controller.Tick(0.5f)); // would have finished under the old, shorter duration
        }

        [Test]
        public void Cancel_Deactivates()
        {
            var controller = new TimeFeedbackController();
            controller.TryPlay(Config(1f), FeedbackPriority.Normal);

            controller.Cancel();

            Assert.IsFalse(controller.IsActive);
        }
    }
}
