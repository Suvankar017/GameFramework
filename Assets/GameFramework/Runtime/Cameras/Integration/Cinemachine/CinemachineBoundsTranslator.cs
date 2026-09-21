using GameFramework.Cameras.Configuration;
using UnityEngine;

namespace GameFramework.Cameras.CinemachineIntegration
{
    /// <summary>
    /// Pure translation from a <see cref="CameraBoundsSettings"/> rect to the center/size a
    /// <see cref="UnityEngine.BoxCollider2D"/> needs - the one piece of genuinely new math this
    /// integration adds, and only because <c>Cinemachine.CinemachineConfiner2D</c> confines against a
    /// <see cref="Collider2D"/> shape, not a raw min/max rect. Cinemachine still performs all of the
    /// actual confinement (path-finding around the shape, camera-window sizing); this only produces
    /// the shape for it to confine against, and only when a project has not already authored one on
    /// the prefab (see <see cref="CinemachineCameraAdapter"/>'s remarks on respecting prefab
    /// authority). Kept pure/static specifically so this translation is unit-testable without a live
    /// scene, mirroring every other pure-math piece in <see cref="GameFramework.Cameras"/> (e.g.
    /// <c>CameraBoundsConstraint</c>).
    /// </summary>
    internal static class CinemachineBoundsTranslator
    {
        /// <summary>False (and both outputs default) when <paramref name="bounds"/> is null, disabled,
        /// or degenerate (zero/negative width or height) - callers must treat that as "nothing to
        /// generate," not fall back to some default shape.</summary>
        public static bool TryComputeBoxBounds(CameraBoundsSettings bounds, out Vector2 center, out Vector2 size)
        {
            center = Vector2.zero;
            size = Vector2.zero;

            if (bounds == null || !bounds.Enabled)
            {
                return false;
            }

            float width = bounds.MaxX - bounds.MinX;
            float height = bounds.MaxY - bounds.MinY;

            if (width <= 0f || height <= 0f)
            {
                return false;
            }

            center = new Vector2((bounds.MinX + bounds.MaxX) * 0.5f, (bounds.MinY + bounds.MaxY) * 0.5f);
            size = new Vector2(width, height);
            return true;
        }
    }
}
