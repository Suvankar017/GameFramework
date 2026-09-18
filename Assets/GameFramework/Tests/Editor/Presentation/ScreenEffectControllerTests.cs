using GameFramework.Presentation.Configs;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Presentation.Tests
{
    public class ScreenEffectControllerTests
    {
        private static ScreenEffectFeedbackConfig Config(float fadeIn = 0f, float hold = 0f, float fadeOut = 0f, float alpha = 1f) =>
            new ScreenEffectFeedbackConfig { FadeInSeconds = fadeIn, HoldSeconds = hold, FadeOutSeconds = fadeOut, Color = new Color(1f, 0f, 0f, alpha) };

        [Test]
        public void TryPlay_NoFades_ActivatesAtPeakAlphaImmediately()
        {
            var controller = new ScreenEffectController();

            bool accepted = controller.TryPlay(Config(), 1f, FeedbackPriority.Normal);

            Assert.IsTrue(accepted);
            Assert.IsTrue(controller.IsActive);
            Assert.AreEqual(1f, controller.CurrentAlpha);
        }

        [Test]
        public void TryPlay_WithFadeIn_StartsAtZeroAlpha()
        {
            var controller = new ScreenEffectController();

            controller.TryPlay(Config(fadeIn: 0.5f), 1f, FeedbackPriority.Normal);

            Assert.AreEqual(0f, controller.CurrentAlpha);
        }

        [Test]
        public void Tick_DuringFadeIn_RampsTowardPeak()
        {
            var controller = new ScreenEffectController();
            controller.TryPlay(Config(fadeIn: 1f), 1f, FeedbackPriority.Normal);

            controller.Tick(0.5f);

            Assert.AreEqual(0.5f, controller.CurrentAlpha, 0.001f);
        }

        [Test]
        public void Tick_ThroughFullSequence_EndsInactive()
        {
            var controller = new ScreenEffectController();
            controller.TryPlay(Config(fadeIn: 0.1f, hold: 0.1f, fadeOut: 0.1f), 1f, FeedbackPriority.Normal);

            for (int i = 0; i < 40; i++)
            {
                controller.Tick(0.01f);
            }

            Assert.IsFalse(controller.IsActive);
            Assert.AreEqual(0f, controller.CurrentAlpha);
        }

        [Test]
        public void Tick_DuringHold_StaysAtPeakAlpha()
        {
            var controller = new ScreenEffectController();
            controller.TryPlay(Config(fadeIn: 0f, hold: 1f, fadeOut: 0f), 1f, FeedbackPriority.Normal);

            controller.Tick(0.5f);

            Assert.AreEqual(1f, controller.CurrentAlpha);
            Assert.IsTrue(controller.IsActive);
        }

        [Test]
        public void Intensity_ScalesPeakAlpha()
        {
            var controller = new ScreenEffectController();

            controller.TryPlay(Config(alpha: 1f), 0.5f, FeedbackPriority.Normal);

            Assert.AreEqual(0.5f, controller.CurrentAlpha, 0.001f);
        }

        [Test]
        public void TryPlay_LowerPriorityWhileActive_IsRejected()
        {
            var controller = new ScreenEffectController();
            controller.TryPlay(Config(hold: 5f), 1f, FeedbackPriority.High);

            bool accepted = controller.TryPlay(Config(hold: 5f, alpha: 0.2f), 1f, FeedbackPriority.Low);

            Assert.IsFalse(accepted);
            Assert.AreEqual(1f, controller.CurrentAlpha); // untouched - still the High-priority effect
        }

        [Test]
        public void TryPlay_EqualOrHigherPriorityWhileActive_Replaces()
        {
            var controller = new ScreenEffectController();
            controller.TryPlay(Config(hold: 5f), 1f, FeedbackPriority.Normal);

            bool accepted = controller.TryPlay(Config(hold: 5f, alpha: 0.3f), 1f, FeedbackPriority.Normal);

            Assert.IsTrue(accepted);
            Assert.AreEqual(0.3f, controller.CurrentAlpha, 0.001f);
        }

        [Test]
        public void Cancel_ImmediatelyDeactivatesAndZeroesAlpha()
        {
            var controller = new ScreenEffectController();
            controller.TryPlay(Config(hold: 5f), 1f, FeedbackPriority.Normal);

            controller.Cancel();

            Assert.IsFalse(controller.IsActive);
            Assert.AreEqual(0f, controller.CurrentAlpha);
        }
    }
}
