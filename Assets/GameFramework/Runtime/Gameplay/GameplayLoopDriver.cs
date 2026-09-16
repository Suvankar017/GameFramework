using UnityEngine;

namespace GameFramework.Gameplay
{
    /// <summary>
    /// Relays Unity's <c>FixedUpdate</c>/<c>LateUpdate</c> callbacks to the owning
    /// <see cref="GameplayService"/> — a plain C# service class cannot receive Unity lifecycle
    /// callbacks directly, the same problem <c>AudioApplicationLifecycleHook</c> solves for
    /// <c>AudioService</c>. Regular per-frame ticking still goes through
    /// <see cref="Runtime.Services.IUpdatableService.Tick"/> via <c>GameBootstrapper.Update()</c>,
    /// exactly like every other Phase 2/3 service — this driver exists only for the two phases
    /// <see cref="Runtime.Services.IUpdatableService"/> deliberately doesn't cover.
    /// </summary>
    internal sealed class GameplayLoopDriver : MonoBehaviour
    {
        internal GameplayService Owner;

        private void FixedUpdate() => Owner?.TickFixed();

        private void LateUpdate() => Owner?.TickLate();
    }
}
