using UnityEngine;

namespace GameFramework.Core.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Destroys every direct child of this transform. Iterates the full child list and
        /// calls Object.Destroy per child (deferred, like any other Destroy call), so avoid
        /// calling this every frame or on very large hierarchies in a hot path.
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
