using UnityEngine;

namespace GameFramework.Performance.Mobile
{
    /// <summary>Relays Unity's application-lifecycle callbacks to the owning
    /// <see cref="ApplicationLifecycleService"/> - the same pattern <c>GameplayLoopDriver</c> and
    /// <c>AudioApplicationLifecycleHook</c> use for callbacks a plain C# service cannot receive
    /// directly.</summary>
    internal sealed class ApplicationLifecycleDriver : MonoBehaviour
    {
        internal ApplicationLifecycleService Owner;

        private void OnApplicationPause(bool isPaused) => Owner?.HandleApplicationPause(isPaused);

        private void OnApplicationFocus(bool hasFocus) => Owner?.HandleApplicationFocus(hasFocus);

        private void OnApplicationQuit() => Owner?.HandleApplicationQuit();
    }
}
