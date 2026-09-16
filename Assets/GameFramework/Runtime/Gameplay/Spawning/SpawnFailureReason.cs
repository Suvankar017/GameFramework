namespace GameFramework.Gameplay.Spawning
{
    /// <summary>Why a <see cref="Spawner"/> spawn attempt failed — see <see cref="SpawnResult"/>.</summary>
    public enum SpawnFailureReason
    {
        None,
        MissingPrefab,
        InvalidProvider,
        LimitReached,
        Cooldown,
        InvalidParent,
        ProviderFailed,
        SpawnerDestroyed
    }
}
