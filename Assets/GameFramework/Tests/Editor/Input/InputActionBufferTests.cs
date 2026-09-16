using GameFramework.Input;
using NUnit.Framework;

namespace GameFramework.Input.Tests
{
    public class InputActionBufferTests
    {
        [Test]
        public void ConsumeIfBuffered_WithinWindow_ReturnsTrueOnce()
        {
            var buffer = new InputActionBuffer(bufferWindowSeconds: 0.2f);

            buffer.Record(wasPressedThisFrame: true, currentTime: 1.0f);

            Assert.IsTrue(buffer.ConsumeIfBuffered(currentTime: 1.1f));
            Assert.IsFalse(buffer.ConsumeIfBuffered(currentTime: 1.1f)); // already consumed
        }

        [Test]
        public void ConsumeIfBuffered_AfterWindowExpires_ReturnsFalse()
        {
            var buffer = new InputActionBuffer(bufferWindowSeconds: 0.1f);

            buffer.Record(wasPressedThisFrame: true, currentTime: 1.0f);

            Assert.IsFalse(buffer.ConsumeIfBuffered(currentTime: 2.0f));
        }

        [Test]
        public void ConsumeIfBuffered_NeverPressed_ReturnsFalse()
        {
            var buffer = new InputActionBuffer(bufferWindowSeconds: 1f);

            Assert.IsFalse(buffer.ConsumeIfBuffered(currentTime: 0f));
        }
    }
}
