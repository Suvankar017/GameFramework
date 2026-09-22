using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>Default <see cref="IPlatformService"/>. Resolved once at <see cref="Initialize"/> -
    /// platform identity cannot change during a session, so there is nothing to poll.</summary>
    public sealed class PlatformService : IPlatformService
    {
        public PlatformType Platform { get; private set; }

        public bool IsEditor => Platform == PlatformType.Editor;
        public bool IsMobile => Platform == PlatformType.Android || Platform == PlatformType.IOS;
        public bool IsAndroid => Platform == PlatformType.Android;
        public bool IsIOS => Platform == PlatformType.IOS;
        public bool IsDesktop => Platform == PlatformType.Windows || Platform == PlatformType.MacOS || Platform == PlatformType.Linux;

        public void Initialize(IServiceRegistry registry)
        {
            Platform = Resolve(Application.platform);
        }

        public void Shutdown()
        {
        }

        private static PlatformType Resolve(RuntimePlatform platform)
        {
            // Application.isEditor is Unity's own reliable editor flag - checking it first avoids
            // a separate WindowsEditor/OSXEditor/LinuxEditor case for every desktop OS.
            if (Application.isEditor)
            {
                return PlatformType.Editor;
            }

            switch (platform)
            {
                case RuntimePlatform.Android:
                    return PlatformType.Android;
                case RuntimePlatform.IPhonePlayer:
                    return PlatformType.IOS;
                case RuntimePlatform.WindowsPlayer:
                    return PlatformType.Windows;
                case RuntimePlatform.OSXPlayer:
                    return PlatformType.MacOS;
                case RuntimePlatform.LinuxPlayer:
                    return PlatformType.Linux;
                default:
                    return PlatformType.Unknown;
            }
        }
    }
}
