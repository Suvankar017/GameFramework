using UnityEngine;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>Result of a <see cref="Spawner"/> spawn attempt. Never throws for an expected
    /// failure (missing prefab, limit reached, ...) — check <see cref="Success"/> instead.</summary>
    public readonly struct SpawnResult
    {
        public readonly bool Success;
        public readonly GameObject Instance;
        public readonly SpawnFailureReason FailureReason;

        private SpawnResult(bool success, GameObject instance, SpawnFailureReason failureReason)
        {
            Success = success;
            Instance = instance;
            FailureReason = failureReason;
        }

        public static SpawnResult Ok(GameObject instance) => new SpawnResult(true, instance, SpawnFailureReason.None);
        public static SpawnResult Fail(SpawnFailureReason reason) => new SpawnResult(false, null, reason);
    }
}
