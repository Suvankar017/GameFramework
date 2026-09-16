using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Gameplay.Spawning
{
    /// <summary>The default <see cref="ISpawnProvider"/>: plain <see cref="Object.Instantiate"/>/
    /// <see cref="Object.Destroy(Object)"/>, no pooling.</summary>
    public sealed class InstantiateSpawnProvider : ISpawnProvider
    {
        public GameObject Spawn(SpawnRequest request)
        {
            if (request.Prefab == null)
            {
                return null;
            }

            return Object.Instantiate(request.Prefab, request.Position, request.Rotation, request.Parent);
        }

        public void Despawn(GameObject instance)
        {
            if (instance != null)
            {
                Object.Destroy(instance);
            }
        }
    }
}
