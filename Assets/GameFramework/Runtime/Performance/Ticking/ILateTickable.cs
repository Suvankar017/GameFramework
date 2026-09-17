namespace GameFramework.Performance.Ticking
{
    /// <summary>Opt-in for a late-phase callback via <see cref="ITickService"/> (Unity's own
    /// <c>LateUpdate</c>) - useful for camera/presentation adjustments that must run after every
    /// <see cref="ITickable"/> has moved things for the frame. Not a place to move gameplay logic
    /// merely for architectural symmetry - only use it where running after Tick genuinely matters.</summary>
    public interface ILateTickable
    {
        void LateTick(float deltaTime);
    }
}
