using UnityEngine;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// The one centralized place this framework reads <see cref="Application.absoluteURL"/>/
    /// <see cref="Application.deepLinkActivated"/> - see CLAUDE.md's Phase 18 brief, section 21.
    /// Neither API needs a native plugin or <c>#if UNITY_ANDROID</c>/<c>UNITY_IOS</c> branching; both
    /// are genuinely cross-platform Unity engine APIs (Android app links/intents and iOS universal/
    /// custom-scheme links alike funnel through them), the same reasoning
    /// <c>Platform.PermissionService</c> already established for <c>Application.HasUserAuthorization</c>.
    /// <see cref="Awake"/> checks <see cref="Application.absoluteURL"/> once for a cold-start link;
    /// <see cref="Application.deepLinkActivated"/> covers every warm/hot-state link afterward.
    /// </summary>
    internal sealed class DeepLinkCaptureDriver : MonoBehaviour
    {
        internal DeepLinkService Owner;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                Owner?.Process(Application.absoluteURL);
            }

            Application.deepLinkActivated += OnDeepLinkActivated;
        }

        private void OnDeepLinkActivated(string url) => Owner?.Process(url);

        private void OnDestroy() => Application.deepLinkActivated -= OnDeepLinkActivated;
    }
}
