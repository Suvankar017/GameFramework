namespace GameFramework.Performance.Ticking
{
    /// <summary>Opt-in for a fixed-timestep callback via <see cref="ITickService"/>, driven from
    /// Unity's own <c>FixedUpdate</c> so it integrates correctly with physics timing - never call
    /// this from the variable <see cref="ITickable"/> phase.</summary>
    public interface IFixedTickable
    {
        void FixedTick(float fixedDeltaTime);
    }
}
