namespace GameFramework.Audio
{
    /// <summary>Seam between <see cref="AudioCueSampler"/>'s clip/volume/pitch randomization and
    /// <see cref="UnityEngine.Random"/>, so that selection logic is deterministically testable.</summary>
    public interface IRandomSource
    {
        float NextFloat(float minInclusive, float maxInclusive);
        int NextInt(int minInclusive, int maxExclusive);
    }
}
