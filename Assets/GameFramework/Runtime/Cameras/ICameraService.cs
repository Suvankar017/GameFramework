using System;
using GameFramework.Runtime.Services;

namespace GameFramework.Cameras
{
    /// <summary>
    /// Camera orchestration - the framework's single registered entry point (CLAUDE.md's Phase 11
    /// brief, section 5's "primary public entry point"), the Phase 11 equivalent of
    /// <c>GameFlow.IGameFlowService</c>/<c>Presentation.IPresentationService</c>. Owns which
    /// registered <see cref="CameraController"/> is active - base activation plus a temporary
    /// override stack on top of it (section 26) - never owns the actual
    /// <see cref="UnityEngine.Camera"/>/<see cref="UnityEngine.Transform"/> writes themselves; that
    /// is <see cref="CameraDriver"/>'s job (section 3's "Camera Driver" layer).
    ///
    /// Every command is safe against an unknown/stale <see cref="CameraId"/> (returns false/does
    /// nothing rather than throwing) - the same "normal flow never needs a try/catch" policy
    /// <c>GameFlow.IGameFlowService</c> already established.
    /// </summary>
    public interface ICameraService : IGameService
    {
        /// <summary>The currently resolved active camera - the top of the override stack, or the
        /// base activated camera if the stack is empty, or <see cref="CameraId.None"/> if nothing is
        /// registered/activated yet.</summary>
        CameraId ActiveCameraId { get; }

        /// <summary>The controller behind <see cref="ActiveCameraId"/>, or null.</summary>
        CameraController ActiveCamera { get; }

        /// <summary>Raised whenever <see cref="ActiveCameraId"/> changes, as (previous, current) -
        /// the same information published as <see cref="ActiveCameraChangedEvent"/>.</summary>
        event Action<CameraId, CameraId> ActiveCameraChanged;

        /// <summary>Registers <paramref name="controller"/> and assigns it a stable
        /// <see cref="CameraId"/>. Registering the same instance again is idempotent and returns its
        /// existing id (CLAUDE.md's Phase 11 brief, section 6's "safely handle duplicate
        /// registration").</summary>
        CameraId RegisterCamera(CameraController controller, int priority = 0, string owner = null);

        /// <summary>Unregisters <paramref name="id"/>. A no-op for an unknown id. If the removed
        /// camera was active (base or an override), the active camera is recomputed and
        /// <see cref="ActiveCameraChanged"/> fires - never leaves a stale reference (section 6).</summary>
        void UnregisterCamera(CameraId id);

        bool IsRegistered(CameraId id);

        /// <summary>Returns null for an unknown id.</summary>
        CameraController GetController(CameraId id);

        /// <summary>False for an unknown id.</summary>
        bool SetTarget(CameraId id, ICameraTarget target);
        bool ClearTarget(CameraId id);
        bool SetMode(CameraId id, ICameraMode mode);

        /// <summary>Activates <paramref name="id"/> as the base (non-override) camera. Only changes
        /// <see cref="ActiveCameraId"/> immediately if the override stack is currently empty.</summary>
        bool Activate(CameraId id);

        /// <summary>Pushes <paramref name="id"/> as a temporary override - it becomes
        /// <see cref="ActiveCameraId"/> immediately, regardless of priority. Release the returned
        /// handle to remove it; the active camera then falls back to whatever is now the top of the
        /// stack, or the base camera if the stack is empty. Returns null for an unknown id.</summary>
        ICameraOverrideHandle PushOverride(CameraId id);

        /// <summary>Resets a camera's pose to its current mode/target immediately, clearing any
        /// in-progress damping - for restart/respawn/recovery (section 27). False for an unknown id.</summary>
        bool ResetCamera(CameraId id);

        /// <summary>Returns default (an invalid <see cref="CameraRuntimeState.Id"/>) for an unknown id.</summary>
        CameraRuntimeState GetState(CameraId id);
    }
}
