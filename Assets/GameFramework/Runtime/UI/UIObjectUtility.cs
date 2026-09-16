using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>Object.Destroy throws outside Play Mode; this is the one place UI destruction goes
    /// through so screens/popups/the modal blocker are safe to exercise from an EditMode test.</summary>
    internal static class UIObjectUtility
    {
        internal static void DestroySafely(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
