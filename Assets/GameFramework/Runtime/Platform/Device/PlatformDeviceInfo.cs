using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Immutable snapshot of device identity/hardware, captured once by
    /// <see cref="DeviceInfoService"/> - see that type's remarks for why this independently queries
    /// <c>SystemInfo</c> rather than depending on
    /// <see cref="GameFramework.Performance.Mobile.DeviceInfo"/> (a narrower, differently-scoped
    /// static utility from Phase 5). Named <c>PlatformDeviceInfo</c>, not <c>DeviceInfo</c>, to
    /// avoid colliding with that existing type.
    /// </summary>
    public readonly struct PlatformDeviceInfo
    {
        public readonly PlatformType Platform;
        public readonly string Model;
        public readonly string OperatingSystem;
        public readonly DeviceType DeviceType;
        public readonly string ProcessorType;
        public readonly int ProcessorCount;
        public readonly int SystemMemoryMegabytes;
        public readonly string GraphicsDeviceName;
        public readonly int GraphicsMemoryMegabytes;
        public readonly int ScreenWidth;
        public readonly int ScreenHeight;
        public readonly float ScreenDpi;

        public PlatformDeviceInfo(
            PlatformType platform,
            string model,
            string operatingSystem,
            DeviceType deviceType,
            string processorType,
            int processorCount,
            int systemMemoryMegabytes,
            string graphicsDeviceName,
            int graphicsMemoryMegabytes,
            int screenWidth,
            int screenHeight,
            float screenDpi)
        {
            Platform = platform;
            Model = model;
            OperatingSystem = operatingSystem;
            DeviceType = deviceType;
            ProcessorType = processorType;
            ProcessorCount = processorCount;
            SystemMemoryMegabytes = systemMemoryMegabytes;
            GraphicsDeviceName = graphicsDeviceName;
            GraphicsMemoryMegabytes = graphicsMemoryMegabytes;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            ScreenDpi = screenDpi;
        }
    }
}
