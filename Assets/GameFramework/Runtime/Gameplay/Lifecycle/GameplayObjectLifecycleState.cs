namespace GameFramework.Gameplay.Lifecycle
{
    /// <summary>Guard state tracked by <see cref="GameplayObjectLifecycleRunner"/> — never set
    /// directly by <see cref="IGameplayObjectLifecycle"/> implementations.</summary>
    public enum GameplayObjectLifecycleState
    {
        Created,
        Initialized,
        Active,
        Deactivated,
        Destroyed
    }
}
