using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    public sealed class NetworkReachabilityService : INetworkReachabilityService
    {
        public NetworkReachability Current => Application.internetReachability;

        public bool IsOnline => Current != NetworkReachability.NotReachable;

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }
    }
}
