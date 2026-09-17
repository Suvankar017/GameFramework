using UnityEngine;

namespace GameFramework.Performance.Ticking
{
    /// <summary>
    /// Relays Unity's <c>FixedUpdate</c>/<c>LateUpdate</c> to the owning <see cref="TickService"/> -
    /// a plain C# service cannot receive Unity lifecycle callbacks directly, the same problem
    /// <c>GameplayLoopDriver</c> and <c>AudioApplicationLifecycleHook</c> solve for their own
    /// services. The variable <see cref="ITickable"/> phase still goes through
    /// <see cref="Runtime.Services.IUpdatableService.Tick"/> via <c>GameBootstrapper.Update()</c>,
    /// exactly like every other framework service - this driver exists only for the two phases that
    /// mechanism doesn't cover.
    /// </summary>
    internal sealed class TickServiceDriver : MonoBehaviour
    {
        internal TickService Owner;

        private void FixedUpdate() => Owner?.TickFixed();

        private void LateUpdate() => Owner?.TickLate();
    }
}
