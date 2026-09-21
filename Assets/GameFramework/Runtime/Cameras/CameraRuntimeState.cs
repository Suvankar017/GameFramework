namespace GameFramework.Cameras
{
    /// <summary>Read-only runtime snapshot of one registered camera (CLAUDE.md's Phase 11 brief,
    /// section 16) - returned by value, never a live reference into <see cref="CameraService"/>'s
    /// own state, so a caller can never mutate the framework's internals through it.</summary>
    public readonly struct CameraRuntimeState
    {
        public readonly CameraId Id;
        public readonly int Priority;
        public readonly string Owner;
        public readonly bool IsActive;
        public readonly bool IsOverride;
        public readonly bool HasTarget;
        public readonly CameraPose Pose;

        public CameraRuntimeState(CameraId id, int priority, string owner, bool isActive, bool isOverride, bool hasTarget, CameraPose pose)
        {
            Id = id;
            Priority = priority;
            Owner = owner;
            IsActive = isActive;
            IsOverride = isOverride;
            HasTarget = hasTarget;
            Pose = pose;
        }
    }
}
