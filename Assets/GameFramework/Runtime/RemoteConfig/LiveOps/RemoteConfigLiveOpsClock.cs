using System;

namespace GameFramework.RemoteConfig.LiveOps
{
    /// <summary>Default <see cref="ILiveOpsClock"/> - local UTC time adjusted by
    /// <see cref="IRemoteConfigService.ServerTimeOffset"/> (zero when no provider has ever supplied a
    /// server timestamp, in which case this is exactly local time - see CLAUDE.md's Phase 17 brief,
    /// section 39: "local time with explicit limitations").</summary>
    public sealed class RemoteConfigLiveOpsClock : ILiveOpsClock
    {
        private readonly IRemoteConfigService _remoteConfig;

        public RemoteConfigLiveOpsClock(IRemoteConfigService remoteConfig)
        {
            _remoteConfig = remoteConfig ?? throw new ArgumentNullException(nameof(remoteConfig));
        }

        public DateTime UtcNow => DateTime.UtcNow + _remoteConfig.ServerTimeOffset;
    }
}
