namespace GameFramework.Gameplay
{
    /// <summary>Opt-in for a gameplay object driven by the loop's FixedUpdate-equivalent phase
    /// (physics-affecting logic) — see <see cref="IGameplayService.RegisterFixedTickable"/>. Same
    /// pause behavior as <see cref="IGameplayTickable"/>.</summary>
    public interface IGameplayFixedTickable
    {
        /// <summary><paramref name="fixedDeltaTime"/> is <see cref="Runtime.Time.ITimeService.FixedDeltaTime"/>.</summary>
        void GameplayFixedTick(float fixedDeltaTime);
    }
}
