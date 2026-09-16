using UnityEngine;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>Designer-authored spawn preset. A <see cref="Spawner"/> with one of these assigned
    /// pulls its prefab and limits from here instead of its own serialized fields.</summary>
    [CreateAssetMenu(menuName = "GameFramework/Gameplay/Spawn Configuration", fileName = "SpawnConfiguration")]
    public sealed class SpawnConfiguration : ScriptableObject
    {
        public GameObject Prefab;
        [Min(0)] public int MaxActiveInstances;
        [Min(0)] public int MaxTotalInstances;
        [Min(0f)] public float SpawnCooldownSeconds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Prefab == null)
            {
                Debug.LogWarning($"[SpawnConfiguration] '{name}' has no Prefab assigned.", this);
            }
        }
#endif
    }
}
