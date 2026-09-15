using UnityEngine;

namespace GameFramework.Core.Extensions
{
    /// <summary>
    /// Null-safety helpers for <see cref="Object"/> references.
    /// A destroyed UnityEngine.Object is not a C# null reference, but Unity overloads ==
    /// so it compares equal to null. That overload only resolves when the static type of the
    /// expression is UnityEngine.Object (or a subclass); it is silently skipped in generic code
    /// and behind interfaces, which is a common source of stale-reference bugs. These extension
    /// methods pin the static type to Object so the overload always applies.
    /// </summary>
    public static class UnityObjectExtensions
    {
        public static bool IsNullOrDestroyed(this Object unityObject)
        {
            return unityObject == null;
        }

        public static bool IsAlive(this Object unityObject)
        {
            return unityObject != null;
        }
    }
}
