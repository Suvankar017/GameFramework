namespace GameFramework.Gameplay
{
    /// <summary>
    /// Opt-in for a gameplay object that wants to be driven by the centralized gameplay loop's
    /// Update-equivalent phase instead of its own <c>MonoBehaviour.Update</c> — see
    /// <see cref="IGameplayService.RegisterTickable"/>. Ticked only while
    /// <see cref="IGameplayService.State"/> is <see cref="GameplayLoopState.Playing"/>: paused or
    /// inactive sessions simply stop calling this, rather than calling it with a zero delta.
    /// Kept separate from <see cref="IGameplayFixedTickable"/>/<see cref="IGameplayLateTickable"/>
    /// so a participant only implements the phase(s) it actually needs.
    /// </summary>
    public interface IGameplayTickable
    {
        /// <summary><paramref name="deltaTime"/> is <see cref="Runtime.Time.ITimeService.ScaledDeltaTime"/>.</summary>
        void GameplayTick(float deltaTime);
    }
}
