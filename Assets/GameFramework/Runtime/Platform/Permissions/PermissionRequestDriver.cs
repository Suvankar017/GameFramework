using System;
using System.Collections;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>Runs the coroutine <see cref="PermissionService"/> (a plain C# class) needs to wait
    /// for an <see cref="AsyncOperation"/> to complete - the same driver-MonoBehaviour pattern
    /// <c>Performance.Mobile.ApplicationLifecycleDriver</c> uses for callbacks/async work a plain C#
    /// service cannot receive or await directly.</summary>
    internal sealed class PermissionRequestDriver : MonoBehaviour
    {
        internal void WaitForResult(AsyncOperation operation, Action onComplete)
        {
            StartCoroutine(Wait(operation, onComplete));
        }

        private static IEnumerator Wait(AsyncOperation operation, Action onComplete)
        {
            yield return operation;
            onComplete?.Invoke();
        }
    }
}
