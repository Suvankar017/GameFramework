using System;
using UnityEngine;

namespace GameFramework.Audio
{
    /// <summary>
    /// The one MonoBehaviour <see cref="AudioService"/> needs purely to receive
    /// <c>OnApplicationPause</c> — a plain C# service class cannot receive Unity lifecycle
    /// callbacks directly. Carries no logic of its own; it only forwards to the service.
    /// </summary>
    internal sealed class AudioApplicationLifecycleHook : MonoBehaviour
    {
        internal event Action<bool> ApplicationPauseChanged;

        private void OnApplicationPause(bool pauseStatus) => ApplicationPauseChanged?.Invoke(pauseStatus);
    }
}
