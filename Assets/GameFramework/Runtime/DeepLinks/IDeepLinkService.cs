using System;
using GameFramework.Runtime.Services;

namespace GameFramework.DeepLinks
{
    /// <summary>
    /// Game-facing deep-link API - see CLAUDE.md's Phase 18 brief, sections 18/20/44. Captures cold/
    /// warm/hot incoming URIs automatically (via <see cref="UnityEngine.Application.absoluteURL"/>/
    /// <see cref="UnityEngine.Application.deepLinkActivated"/>) and routes them to registered
    /// <see cref="IDeepLinkHandler"/>s - this framework never navigates/instantiates UI itself
    /// (sections 20/24).
    /// </summary>
    public interface IDeepLinkService : IGameService
    {
        /// <summary>False until the game calls <see cref="SetReady"/> - see CLAUDE.md's Phase 18
        /// brief, section 22. A link that arrives while not ready is held as
        /// <see cref="PendingRawUri"/> instead of being dispatched.</summary>
        bool IsReady { get; }

        /// <summary>The one link currently deferred awaiting <see cref="SetReady"/>, or null/empty if
        /// none. Only ever holds the most recent deferred link - see <see cref="DeepLinkService"/>'s
        /// remarks.</summary>
        string PendingRawUri { get; }

        event Action<string> DeepLinkReceived;
        event Action<string> DeepLinkHandled;
        event Action<string, string> DeepLinkRejected;

        /// <summary>Registers <paramref name="handler"/>. Higher <paramref name="priority"/> is tried
        /// first; handlers of equal priority are tried in registration order (section 20).</summary>
        void RegisterHandler(IDeepLinkHandler handler, int priority = 0);

        void UnregisterHandler(IDeepLinkHandler handler);

        /// <summary>
        /// Processes an incoming raw URI - parses, validates, dedupes, defers (if not yet
        /// <see cref="IsReady"/>), and routes it through registered handlers in priority order. Never
        /// throws; every outcome is reported via <see cref="DeepLinkResult"/> (section 45). Called
        /// automatically for cold/warm/hot incoming links; also safe to call directly (e.g. from an
        /// Editor debug tool, or a notification-tap bridge).
        /// </summary>
        DeepLinkResult Process(string rawUri);

        /// <summary>Marks the framework/UI/GameFlow as ready to actually act on a link (section 22/40).
        /// Setting true immediately dispatches <see cref="PendingRawUri"/>, if any, exactly once.</summary>
        void SetReady(bool ready = true);

        DeepLinkDiagnostics GetDiagnostics();
    }
}
