namespace GameFramework.Performance.Ticking
{
    /// <summary>
    /// Opt-in for an object that wants a per-frame callback without writing its own
    /// <c>MonoBehaviour.Update</c> - register it with <see cref="ITickService"/> instead. Unlike
    /// <see cref="GameFramework.Gameplay.IGameplayTickable"/> (which stops firing entirely while a
    /// gameplay session is paused), this ticks every frame with
    /// <see cref="Runtime.Time.ITimeService.ScaledDeltaTime"/>, which is itself 0 while
    /// <see cref="Runtime.Time.ITimeService.IsPaused"/> - the same relationship Unity's own
    /// <c>Update</c> + <c>Time.deltaTime</c> has to pausing via <c>Time.timeScale</c>. Use
    /// <see cref="GameFramework.Gameplay.IGameplayTickable"/> instead when a gameplay session
    /// pausing should stop this object being ticked at all.
    /// </summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
