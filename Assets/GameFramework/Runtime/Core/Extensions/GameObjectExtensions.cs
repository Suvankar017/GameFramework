using UnityEngine;

namespace GameFramework.Core.Extensions
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Returns the first <typeparamref name="T"/> on this GameObject, adding one first
        /// if none is present.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component)
                ? component
                : gameObject.AddComponent<T>();
        }
    }
}
