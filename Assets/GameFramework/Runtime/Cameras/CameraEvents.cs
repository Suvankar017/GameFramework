namespace GameFramework.Cameras
{
    /// <summary>Published by <see cref="CameraService"/> through the Phase 2 Event System - no
    /// separate notification mechanism, matching every other cross-system notification in the
    /// framework (compare <c>GameFlow.LevelFlowStateChangedEvent</c>).</summary>
    public readonly struct CameraRegisteredEvent
    {
        public readonly CameraId Id;
        public CameraRegisteredEvent(CameraId id) => Id = id;
    }

    public readonly struct CameraUnregisteredEvent
    {
        public readonly CameraId Id;
        public CameraUnregisteredEvent(CameraId id) => Id = id;
    }

    /// <summary>Raised whenever the resolved active camera (base activation, or the top of the
    /// override stack) changes - the single place to react to "which camera is live now" instead of
    /// polling <see cref="ICameraService.ActiveCameraId"/> every frame.</summary>
    public readonly struct ActiveCameraChangedEvent
    {
        public readonly CameraId Previous;
        public readonly CameraId Current;

        public ActiveCameraChangedEvent(CameraId previous, CameraId current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct CameraTargetChangedEvent
    {
        public readonly CameraId Id;
        public CameraTargetChangedEvent(CameraId id) => Id = id;
    }

    public readonly struct CameraOverridePushedEvent
    {
        public readonly CameraId Id;
        public CameraOverridePushedEvent(CameraId id) => Id = id;
    }

    public readonly struct CameraOverridePoppedEvent
    {
        public readonly CameraId Id;
        public CameraOverridePoppedEvent(CameraId id) => Id = id;
    }

    /// <summary>Published by <see cref="CameraService.ResetCamera"/> - lets an external integration
    /// that maintains its own parallel damped state alongside a <see cref="CameraController"/> (e.g.
    /// a Cinemachine backend's own lens-zoom damping) snap that state too, rather than only the
    /// controller's own pipeline resetting.</summary>
    public readonly struct CameraResetEvent
    {
        public readonly CameraId Id;
        public CameraResetEvent(CameraId id) => Id = id;
    }
}
