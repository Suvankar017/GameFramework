using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Centralized device information - see CLAUDE.md's Phase 14 brief, sections 7-9.
    /// <see cref="Current"/> is captured once (in <see cref="IGameService.Initialize"/>); battery is
    /// queried live since it genuinely changes over a session and
    /// <see cref="UnityEngine.SystemInfo.batteryLevel"/> is itself a cheap OS query, not something
    /// worth caching/polling for (see <see cref="BatteryLevel"/>).
    /// </summary>
    public interface IDeviceInfoService : IGameService
    {
        PlatformDeviceInfo Current { get; }

        /// <summary>-1 when the platform does not report battery level (e.g. desktop/editor).</summary>
        float BatteryLevel { get; }

        BatteryStatus BatteryStatus { get; }

        bool Supports(DeviceCapability capability);
    }
}
