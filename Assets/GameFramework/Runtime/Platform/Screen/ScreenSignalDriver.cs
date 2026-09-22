using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>Relays a per-frame Update to <see cref="ScreenService.PollForChanges"/> - see that
    /// type's remarks for why polling is necessary here. The comparison itself is two cheap
    /// value-type equality checks (see CLAUDE.md's Tick Rules: "if an update loop is necessary, keep
    /// it cheap").</summary>
    internal sealed class ScreenSignalDriver : MonoBehaviour
    {
        internal ScreenService Owner;

        private void Update() => Owner?.PollForChanges();
    }
}
