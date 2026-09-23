using System.Collections.Generic;

namespace GameFramework.Notifications
{
    /// <summary>
    /// Structural validation for an inbound <see cref="NotificationPayload"/> - a payload delivered by
    /// the OS/a provider is untrusted external input (CLAUDE.md's Phase 19 section, "Notification payload
    /// validation"). <see cref="NotificationService"/> applies this to every opened/received
    /// notification before publishing it; an invalid payload is replaced by
    /// <see cref="NotificationPayload.Empty"/> (the app was still genuinely opened from the notification,
    /// so the open itself is still reported - only its routing intent is dropped).
    ///
    /// Like deep links, a payload can only <i>request</i> navigation - it must never grant currency,
    /// entitlements, purchases, or progress. What a route/parameter <i>means</i> stays the game's
    /// (handler's) decision; this only rejects shapes no legitimate payload has.
    /// </summary>
    public static class NotificationPayloadValidator
    {
        /// <summary>The newest <see cref="NotificationPayload.Version"/> this framework build
        /// understands. A payload stamped with a newer version (sent by a newer build/backend) is
        /// rejected rather than guessed at.</summary>
        public const int MaxSupportedVersion = 1;

        public const int MaxRouteLength = 256;
        public const int MaxParameters = 32;
        public const int MaxParameterKeyLength = 64;
        public const int MaxParameterValueLength = 1024;

        /// <summary>Bound on each of the free-form fields (Type/ContentId/Action/Source).</summary>
        public const int MaxFieldLength = 256;

        /// <summary>Returns false with a log-safe reason (never echoes parameter values).</summary>
        public static bool Validate(NotificationPayload payload, out string reason)
        {
            if (payload == null)
            {
                reason = "Payload is null.";
                return false;
            }

            if (payload.Version < 1 || payload.Version > MaxSupportedVersion)
            {
                reason = $"Unsupported payload version {payload.Version} (supported: 1-{MaxSupportedVersion}).";
                return false;
            }

            if (!IsValidRoute(payload.Route))
            {
                reason = "Route is too long or contains characters outside [A-Za-z0-9-_./~].";
                return false;
            }

            if (payload.Type.Length > MaxFieldLength || payload.ContentId.Length > MaxFieldLength ||
                payload.Action.Length > MaxFieldLength || payload.Source.Length > MaxFieldLength)
            {
                reason = $"A payload field exceeds {MaxFieldLength} characters.";
                return false;
            }

            if (payload.Parameters.Count > MaxParameters)
            {
                reason = $"Payload has more than {MaxParameters} parameters.";
                return false;
            }

            foreach (KeyValuePair<string, string> parameter in payload.Parameters)
            {
                if (string.IsNullOrEmpty(parameter.Key) || parameter.Key.Length > MaxParameterKeyLength ||
                    (parameter.Value?.Length ?? 0) > MaxParameterValueLength)
                {
                    reason = "A payload parameter key is empty/oversized or its value is oversized.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        /// <summary>An empty route is valid ("just open the app"). Otherwise a route is a path-like
        /// token - the same character set a deep-link path segment uses - so it can be embedded in a
        /// synthetic URI (<c>Notifications.Integration.NotificationDeepLinkBridge</c>) without
        /// injecting a scheme, query, fragment, or authority.</summary>
        public static bool IsValidRoute(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                return true;
            }

            if (route.Length > MaxRouteLength || route.Contains(".."))
            {
                return false;
            }

            for (int i = 0; i < route.Length; i++)
            {
                char c = route[i];
                bool allowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                               c == '-' || c == '_' || c == '.' || c == '/' || c == '~';
                if (!allowed)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
