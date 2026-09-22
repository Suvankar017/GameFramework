using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Default <see cref="IDeviceInfoService"/>. Deliberately independent of
    /// <see cref="GameFramework.Performance.Mobile.DeviceInfo"/> (Phase 5's static utility): that
    /// type exists only to hint at a coarse <c>PerformanceProfile</c> choice and is explicitly
    /// scoped to Performance's own concerns; this service is the general-purpose device/capability
    /// surface Phase 14 requires and must stay usable by a game that does not use
    /// <c>GameFramework.Performance</c> at all - the same "usable by any game regardless of which
    /// other systems it also uses" independence Phase 5 and Phase 13 already established for
    /// themselves (see <c>PlayerDataLifecycleDriver</c>'s remarks for the precedent this follows).
    /// The handful of duplicated <c>SystemInfo</c> reads this costs are one-time, at
    /// <see cref="Initialize"/>, not a hot-path concern.
    /// </summary>
    public sealed class DeviceInfoService : IDeviceInfoService
    {
        public PlatformDeviceInfo Current { get; private set; }

        public float BatteryLevel => SystemInfo.batteryLevel;

        public BatteryStatus BatteryStatus => SystemInfo.batteryStatus;

        public void Initialize(IServiceRegistry registry)
        {
            var platformService = registry.Get<IPlatformService>();

            Current = new PlatformDeviceInfo(
                platformService.Platform,
                SystemInfo.deviceModel,
                SystemInfo.operatingSystem,
                SystemInfo.deviceType,
                SystemInfo.processorType,
                SystemInfo.processorCount,
                SystemInfo.systemMemorySize,
                SystemInfo.graphicsDeviceName,
                SystemInfo.graphicsMemorySize,
                Screen.width,
                Screen.height,
                Screen.dpi);
        }

        public void Shutdown()
        {
        }

        public bool Supports(DeviceCapability capability)
        {
            switch (capability)
            {
                case DeviceCapability.Haptics:
                    // Matches Feedback.MobileHapticProvider.IsSupported exactly - same platform
                    // question, asked from the device-info side instead of the haptics side.
                    return Application.isMobilePlatform;
                case DeviceCapability.Gyroscope:
                    return SystemInfo.supportsGyroscope;
                case DeviceCapability.Accelerometer:
                    return SystemInfo.supportsAccelerometer;
                case DeviceCapability.Touch:
                    return Input.touchSupported;
                case DeviceCapability.MultiTouch:
                    return Input.touchSupported && SystemInfo.deviceType == DeviceType.Handheld;
                case DeviceCapability.LocationService:
                    return SystemInfo.supportsLocationService;
                case DeviceCapability.Clipboard:
                    return true;
                case DeviceCapability.Audio:
                    return SystemInfo.supportsAudio;
                default:
                    return false;
            }
        }
    }
}
