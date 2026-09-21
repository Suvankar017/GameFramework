using GameFramework.Cameras.Modes;
using UnityEngine;

namespace GameFramework.Cameras.Configuration
{
    /// <summary>
    /// Reusable, shared authoring data for a gameplay camera (CLAUDE.md's Phase 11 brief, sections
    /// 30-31) - mode selection plus one settings block per concern (Follow/TargetLook/Zoom/Bounds/
    /// Transition), the same "fixed set of typed fields, never runtime state" shape
    /// <c>Presentation.FeedbackDefinition</c> already established for its own authored bundle. The
    /// same asset is shared by every <see cref="CameraController"/> that references it - runtime
    /// state (current target, current pose, current mode instance) always lives on the controller,
    /// never here, so two controllers sharing one configuration never corrupt each other.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFramework/Cameras/Camera Configuration", fileName = "CameraConfiguration")]
    public sealed class CameraConfiguration : ScriptableObject
    {
        [SerializeField] private CameraModeKind _mode = CameraModeKind.Follow;

        [SerializeField] private CameraFollowSettings _follow = new CameraFollowSettings();
        [SerializeField] private CameraTargetLookSettings _targetLook = new CameraTargetLookSettings();
        [SerializeField] private CameraZoomSettings _zoom = new CameraZoomSettings();
        [SerializeField] private CameraBoundsSettings _bounds = new CameraBoundsSettings();
        [SerializeField] private CameraTransitionSettings _defaultTransition = new CameraTransitionSettings();

        public CameraModeKind Mode => _mode;

        public CameraFollowSettings Follow => _follow;
        public CameraTargetLookSettings TargetLook => _targetLook;
        public CameraZoomSettings Zoom => _zoom;
        public CameraBoundsSettings Bounds => _bounds;
        public CameraTransitionSettings DefaultTransition => _defaultTransition;

        /// <summary>Builds a fresh <see cref="ICameraMode"/> instance matching <see cref="Mode"/> -
        /// called once by <see cref="CameraController.Awake"/>. Each controller gets its own mode
        /// instance (modes carry per-controller damping velocity state) even when several
        /// controllers share one <see cref="CameraConfiguration"/> asset.</summary>
        public ICameraMode CreateMode()
        {
            switch (_mode)
            {
                case CameraModeKind.Follow:
                    return new FollowCameraMode(_follow);
                case CameraModeKind.TargetLook:
                    return new TargetLookCameraMode(_targetLook);
                case CameraModeKind.Manual:
                    return new ManualCameraMode();
                case CameraModeKind.Static:
                default:
                    return new StaticCameraMode();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_zoom.MinOrthographicSize > _zoom.MaxOrthographicSize)
            {
                Debug.LogWarning($"[CameraConfiguration] '{name}' has MinOrthographicSize > MaxOrthographicSize.", this);
            }

            if (_zoom.MinFieldOfView > _zoom.MaxFieldOfView)
            {
                Debug.LogWarning($"[CameraConfiguration] '{name}' has MinFieldOfView > MaxFieldOfView.", this);
            }

            if (_bounds.Enabled && (_bounds.MinX > _bounds.MaxX || _bounds.MinY > _bounds.MaxY))
            {
                Debug.LogWarning($"[CameraConfiguration] '{name}' has an inverted Bounds rect (Min > Max).", this);
            }
        }
#endif
    }
}
