using GameFramework.Gameplay.Entities;
using NUnit.Framework;

namespace GameFramework.Gameplay.Tests
{
    public class EntityIdTests
    {
        [Test]
        public void None_IsInvalid()
        {
            Assert.IsFalse(EntityId.None.IsValid);
        }

        [Test]
        public void New_IsValid()
        {
            Assert.IsTrue(EntityId.New().IsValid);
        }

        [Test]
        public void New_ReturnsUniqueValues()
        {
            EntityId a = EntityId.New();
            EntityId b = EntityId.New();

            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void Equality_IsValueBased()
        {
            EntityId a = EntityId.New();
            EntityId copy = a;

            Assert.AreEqual(a, copy);
            Assert.IsTrue(a == copy);
            Assert.IsFalse(a != copy);
        }
    }
}
