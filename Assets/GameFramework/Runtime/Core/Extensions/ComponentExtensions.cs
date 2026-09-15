using UnityEngine;

namespace GameFramework.Core.Extensions
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Returns the first <typeparamref name="T"/> on this Component's GameObject, adding
        /// one first if none is present.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }
    }
}
