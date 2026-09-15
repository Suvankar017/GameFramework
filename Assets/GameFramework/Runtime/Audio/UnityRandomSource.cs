using UnityEngine;

namespace GameFramework.Audio
{
    public sealed class UnityRandomSource : IRandomSource
    {
        public float NextFloat(float minInclusive, float maxInclusive) => Random.Range(minInclusive, maxInclusive);
        public int NextInt(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    }
}
