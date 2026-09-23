using System;
using System.Collections.Generic;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Provider-independent routing/intent payload - see CLAUDE.md's Phase 18 brief, section 8. The
    /// framework only transports this; it never interprets <see cref="Route"/> or
    /// <see cref="Parameters"/> itself (that is <c>DeepLinks.IDeepLinkHandler</c>/the game's job - see
    /// <c>Notifications.Integration.NotificationDeepLinkBridge</c>). Deliberately not JSON-serialized
    /// by this phase (CLAUDE.md's Phase 18 brief, section 49: "avoid unnecessary JSON serialization")
    /// - the shipped NoOp/Mock providers keep it as a plain in-memory object; a real provider adapter
    /// owns whatever wire format it actually needs to round-trip this through the OS.
    /// </summary>
    public sealed class NotificationPayload
    {
        public string Type { get; }
        public string Route { get; }
        public IReadOnlyDictionary<string, string> Parameters { get; }
        public string ContentId { get; }
        public string Action { get; }
        public string Source { get; }
        public int Version { get; }

        public NotificationPayload(
            string type = null,
            string route = null,
            IReadOnlyDictionary<string, string> parameters = null,
            string contentId = null,
            string action = null,
            string source = null,
            int version = 1)
        {
            Type = type ?? string.Empty;
            Route = route ?? string.Empty;
            Parameters = parameters ?? new Dictionary<string, string>(0);
            ContentId = contentId ?? string.Empty;
            Action = action ?? string.Empty;
            Source = source ?? string.Empty;
            Version = version;
        }

        public static readonly NotificationPayload Empty = new NotificationPayload();
    }
}
