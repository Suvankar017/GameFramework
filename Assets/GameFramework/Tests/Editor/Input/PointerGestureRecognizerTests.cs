using GameFramework.Input;
using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Input.Tests
{
    public class PointerGestureRecognizerTests
    {
        private PointerGestureRecognizer _recognizer;

        [SetUp]
        public void SetUp()
        {
            _recognizer = new PointerGestureRecognizer();
        }

        private static PointerState Down(Vector2 position, bool pressedThisFrame) =>
            new PointerState(-1, position, true, pressedThisFrame, false, false);

        private static PointerState Up(Vector2 position) =>
            new PointerState(-1, position, false, false, true, false);

        [Test]
        public void QuickTapWithinThresholds_RaisesTapped()
        {
            Vector2? tapPosition = null;
            _recognizer.Tapped += p => tapPosition = p;

            _recognizer.Update(Down(new Vector2(10, 10), true), 0f);
            _recognizer.Update(Up(new Vector2(12, 11)), 0.1f);

            Assert.IsTrue(tapPosition.HasValue);
        }

        [Test]
        public void LongHold_RaisesLongPressed()
        {
            bool longPressed = false;
            _recognizer.LongPressed += _ => longPressed = true;

            _recognizer.Update(Down(new Vector2(10, 10), true), 0f);
            _recognizer.Update(Down(new Vector2(10, 10), false), 0.7f);

            Assert.IsTrue(longPressed);
        }

        [Test]
        public void FastLongDistanceMove_RaisesSwipe()
        {
            bool swiped = false;
            _recognizer.Update(Down(new Vector2(0, 0), true), 0f);
            _recognizer.Update(Down(new Vector2(100, 0), false), 0.1f);
            _recognizer.Swiped += _ => swiped = true;
            _recognizer.Update(Up(new Vector2(200, 0)), 0.15f);

            Assert.IsTrue(swiped);
        }

        [Test]
        public void SlowSmallMove_DoesNotRaiseTapWhenTooSlow()
        {
            bool tapped = false;
            _recognizer.Tapped += _ => tapped = true;

            _recognizer.Update(Down(new Vector2(0, 0), true), 0f);
            _recognizer.Update(Up(new Vector2(1, 1)), 1f); // exceeds TapMaxDuration

            Assert.IsFalse(tapped);
        }
    }
}
