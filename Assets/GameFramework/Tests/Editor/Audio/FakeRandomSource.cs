namespace GameFramework.Audio.Tests
{
    /// <summary>Deterministic <see cref="IRandomSource"/> — always returns the configured fixed
    /// values instead of a real random draw.</summary>
    internal sealed class FakeRandomSource : IRandomSource
    {
        public float FixedFloat;
        public int FixedInt;

        public float NextFloat(float minInclusive, float maxInclusive) => FixedFloat;
        public int NextInt(int minInclusive, int maxExclusive) => FixedInt;
    }
}
