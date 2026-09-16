namespace GameFramework.Gameplay
{
    /// <summary>Opt-in for a gameplay object driven by the loop's LateUpdate-equivalent phase
    /// (e.g. camera-follow logic that must run after movement) — see
    /// <see cref="IGameplayService.RegisterLateTickable"/>. Same pause behavior as
    /// <see cref="IGameplayTickable"/>.</summary>
    public interface IGameplayLateTickable
    {
        /// <summary><paramref name="deltaTime"/> is <see cref="Runtime.Time.ITimeService.ScaledDeltaTime"/>.</summary>
        void GameplayLateTick(float deltaTime);
    }
}
