using NUnit.Framework;

namespace GameFramework.Unlocks.Tests
{
    public class RequirementCompositionTests
    {
        [Test]
        public void AllRequirement_EverySatisfied_IsSatisfied()
        {
            var requirement = new AllRequirement(new FakeRequirement(true), new FakeRequirement(true));
            Assert.IsTrue(requirement.IsSatisfied());
        }

        [Test]
        public void AllRequirement_OneUnsatisfied_IsNotSatisfied()
        {
            var requirement = new AllRequirement(new FakeRequirement(true), new FakeRequirement(false));
            Assert.IsFalse(requirement.IsSatisfied());
        }

        [Test]
        public void AllRequirement_NoChildren_IsSatisfied()
        {
            var requirement = new AllRequirement();
            Assert.IsTrue(requirement.IsSatisfied());
        }

        [Test]
        public void AnyRequirement_OneSatisfied_IsSatisfied()
        {
            var requirement = new AnyRequirement(new FakeRequirement(false), new FakeRequirement(true));
            Assert.IsTrue(requirement.IsSatisfied());
        }

        [Test]
        public void AnyRequirement_NoneSatisfied_IsNotSatisfied()
        {
            var requirement = new AnyRequirement(new FakeRequirement(false), new FakeRequirement(false));
            Assert.IsFalse(requirement.IsSatisfied());
        }

        [Test]
        public void AnyRequirement_NoChildren_IsNotSatisfied()
        {
            var requirement = new AnyRequirement();
            Assert.IsFalse(requirement.IsSatisfied());
        }

        [Test]
        public void NestedComposition_AndOfOr_EvaluatesCorrectly()
        {
            // (false OR true) AND (true) => true
            var requirement = new AllRequirement(
                new AnyRequirement(new FakeRequirement(false), new FakeRequirement(true)),
                new FakeRequirement(true));

            Assert.IsTrue(requirement.IsSatisfied());
        }
    }
}
