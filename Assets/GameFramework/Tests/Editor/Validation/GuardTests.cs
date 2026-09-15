using System;
using GameFramework.Core.Validation;
using NUnit.Framework;

namespace GameFramework.Core.Tests.Validation
{
    public class GuardTests
    {
        [Test]
        public void NotNull_WithNonNullValue_ReturnsValue()
        {
            var value = new object();

            var result = Guard.NotNull(value, "value");

            Assert.AreSame(value, result);
        }

        [Test]
        public void NotNull_WithNullValue_ThrowsArgumentNullException()
        {
            object value = null;

            Assert.Throws<ArgumentNullException>(() => Guard.NotNull(value, "value"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void NotNullOrEmpty_WithNullOrEmptyValue_ThrowsArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(value, "value"));
        }

        [Test]
        public void NotNullOrEmpty_WithNonEmptyValue_ReturnsValue()
        {
            var result = Guard.NotNullOrEmpty("hello", "value");

            Assert.AreEqual("hello", result);
        }

        [TestCase(5)]
        [TestCase(0)]
        [TestCase(10)]
        public void InRange_WithValueInsideInclusiveRange_ReturnsValue(int value)
        {
            var result = Guard.InRange(value, 0, 10, "value");

            Assert.AreEqual(value, result);
        }

        [TestCase(-1)]
        [TestCase(11)]
        public void InRange_WithValueOutsideRange_ThrowsArgumentOutOfRangeException(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(value, 0, 10, "value"));
        }

        [Test]
        public void IsTrue_WithFalseCondition_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Guard.IsTrue(false, "value"));
        }

        [Test]
        public void IsTrue_WithTrueCondition_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Guard.IsTrue(true, "value"));
        }
    }
}
