using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    /// <summary>
    /// Lightweight reachability query - not a networking framework, and not a proxy for "is our
    /// backend reachable" (see CLAUDE.md's Phase 14 brief, section 30).
    /// <see cref="UnityEngine.Application.internetReachability"/> is itself a cheap OS query, queried
    /// live on demand; no caching/polling driver is introduced for it.
    /// </summary>
    public interface INetworkReachabilityService : IGameService
    {
        NetworkReachability Current { get; }

        bool IsOnline { get; }
    }
}
