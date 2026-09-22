using System;
using GameFramework.Runtime.Diagnostics;
using GameFramework.Runtime.Services;
using UnityEngine;

namespace GameFramework.Platform
{
    public sealed class PlatformUrlService : IPlatformUrlService
    {
        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public bool OpenUrl(string url)
        {
            if (!IsValidUrl(url))
            {
                Log.Warning("Platform", $"Refused to open invalid URL '{url}'.");
                return false;
            }

            Application.OpenURL(url);
            return true;
        }

        /// <summary>Public and pure so both this service and a game's own UI (e.g. to disable a
        /// "visit our website" button for an unconfigured URL) can validate a URL without
        /// triggering <see cref="UnityEngine.Application.OpenURL"/>.</summary>
        public static bool IsValidUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
