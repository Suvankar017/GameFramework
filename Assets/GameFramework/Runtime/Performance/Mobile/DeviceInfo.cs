using UnityEngine;

namespace GameFramework.Performance.Mobile
{
    /// <summary>
    /// Read-once snapshot of device capability, taken the first time any member is accessed.
    /// These are hints for coarse decisions (e.g. picking a default <see cref="PerformanceProfile"/>),
    /// not guarantees - <see cref="SystemMemoryMegabytes"/> in particular is OS-reported and can be
    /// inaccurate or capped on some devices, and none of these values account for other apps
    /// competing for the same resources or for thermal throttling (see the framework
    /// documentation's note on thermal considerations).
    /// </summary>
    public static class DeviceInfo
    {
        private static bool _captured;
        private static RuntimePlatform _platform;
        private static int _processorCount;
        private static int _systemMemoryMegabytes;
        private static int _graphicsMemoryMegabytes;
        private static string _graphicsDeviceName;
        private static int _screenWidth;
        private static int _screenHeight;

        public static RuntimePlatform Platform
        {
            get { EnsureCaptured(); return _platform; }
        }

        public static int ProcessorCount
        {
            get { EnsureCaptured(); return _processorCount; }
        }

        public static int SystemMemoryMegabytes
        {
            get { EnsureCaptured(); return _systemMemoryMegabytes; }
        }

        public static int GraphicsMemoryMegabytes
        {
            get { EnsureCaptured(); return _graphicsMemoryMegabytes; }
        }

        public static string GraphicsDeviceName
        {
            get { EnsureCaptured(); return _graphicsDeviceName; }
        }

        public static int ScreenWidth
        {
            get { EnsureCaptured(); return _screenWidth; }
        }

        public static int ScreenHeight
        {
            get { EnsureCaptured(); return _screenHeight; }
        }

        private static void EnsureCaptured()
        {
            if (_captured)
            {
                return;
            }

            _platform = Application.platform;
            _processorCount = SystemInfo.processorCount;
            _systemMemoryMegabytes = SystemInfo.systemMemorySize;
            _graphicsMemoryMegabytes = SystemInfo.graphicsMemorySize;
            _graphicsDeviceName = SystemInfo.graphicsDeviceName;
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _captured = true;
        }
    }
}
