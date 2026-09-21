using System;

namespace GameFramework.Cameras
{
    internal sealed class CameraOverrideHandle : ICameraOverrideHandle
    {
        private readonly Action<CameraOverrideHandle> _onRelease;

        internal CameraOverrideHandle(CameraId cameraId, Action<CameraOverrideHandle> onRelease)
        {
            CameraId = cameraId;
            _onRelease = onRelease;
            IsActive = true;
        }

        public CameraId CameraId { get; }
        public bool IsActive { get; private set; }

        public void Release()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            _onRelease?.Invoke(this);
        }
    }
}
