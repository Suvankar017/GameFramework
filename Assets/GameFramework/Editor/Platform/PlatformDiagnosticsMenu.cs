using GameFramework.Platform;
using GameFramework.Runtime.Bootstrap;
using GameFramework.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor.Platform
{
    /// <summary>
    /// Development-time inspection for Phase 14, following the same "log diagnostics for whichever
    /// GameBootstrapper is currently running" pattern as <c>PlayerDataDiagnosticsMenu</c> - see that
    /// class's remarks for why this project prefers a menu-item log over a full editor window/asset
    /// suite for a phase with no ScriptableObject content to validate.
    /// </summary>
    internal static class PlatformDiagnosticsMenu
    {
        [MenuItem("GameFramework/Platform/Log Diagnostics")]
        private static void LogDiagnostics()
        {
            if (GameBootstrapper.Instance == null || GameBootstrapper.Instance.State != BootstrapState.Ready)
            {
                Debug.LogWarning("[Platform] No ready GameBootstrapper instance found - enter Play Mode first.");
                return;
            }

            IServiceRegistry registry = GameBootstrapper.Instance.Services;
            if (!registry.TryGet(out IPlatformService platform) || !registry.TryGet(out IDeviceInfoService deviceInfo))
            {
                Debug.LogWarning("[Platform] No IPlatformService/IDeviceInfoService is registered on the running GameBootstrapper.");
                return;
            }

            PlatformDeviceInfo info = deviceInfo.Current;

            string screenLine = registry.TryGet(out IScreenService screen)
                ? $", SafeArea={screen.SafeArea}, Orientation={screen.Orientation}"
                : string.Empty;

            string networkLine = registry.TryGet(out INetworkReachabilityService network)
                ? $", Network={network.Current}"
                : string.Empty;

            Debug.Log(
                "[Platform] Platform=" + platform.Platform +
                ", Model=" + info.Model +
                ", OS=" + info.OperatingSystem +
                ", DeviceType=" + info.DeviceType +
                ", CPU=" + info.ProcessorType + " x" + info.ProcessorCount +
                ", Memory=" + info.SystemMemoryMegabytes + "MB" +
                ", GPU=" + info.GraphicsDeviceName + " (" + info.GraphicsMemoryMegabytes + "MB)" +
                ", Screen=" + info.ScreenWidth + "x" + info.ScreenHeight + " @" + info.ScreenDpi + "dpi" +
                ", Battery=" + deviceInfo.BatteryLevel + " (" + deviceInfo.BatteryStatus + ")" +
                screenLine + networkLine);
        }
    }
}
