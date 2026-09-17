namespace GameFramework.Unlocks.Tests
{
    internal sealed class FakeRequirement : IUnlockRequirement
    {
        private readonly bool _satisfied;
        private readonly string _description;

        public FakeRequirement(bool satisfied, string description = "Fake")
        {
            _satisfied = satisfied;
            _description = description;
        }

        public bool IsSatisfied() => _satisfied;
        public string Describe() => _description;
    }
}
